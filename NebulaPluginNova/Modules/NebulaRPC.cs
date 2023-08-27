﻿using HarmonyLib;
using Hazel;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaRPCHolder : Attribute
{

}

public class NebulaRPCInvoker
{

    Action<MessageWriter> sender;
    Action localBodyProcess;
    int hash;

    public NebulaRPCInvoker(int hash, Action<MessageWriter> sender, Action localBodyProcess)
    {
        this.hash = hash;
        this.sender = sender;
        this.localBodyProcess = localBodyProcess;
    }

    public void Invoke(MessageWriter writer)
    {
        writer.Write(hash);
        sender.Invoke(writer);
        localBodyProcess.Invoke();
    }
}

[NebulaPreLoad(true)]
public class RemoteProcessBase
{
    static public Dictionary<int, RemoteProcessBase> AllNebulaProcess = new();


    public int Hash { get; private set; } = -1;
    public string Name { get; private set; }


    public RemoteProcessBase(string name)
    {
        Hash = name.ComputeConstantHash();
        Name = name;

        if (AllNebulaProcess.ContainsKey(Hash)) Debug.Log("[RPC]"+name+" is duplicated!");

        AllNebulaProcess[Hash] = this;
    }

    static public void Load()
    {
        var types = Assembly.GetAssembly(typeof(RemoteProcessBase))?.GetTypes().Where((type) => type.IsDefined(typeof(NebulaRPCHolder)));
        if (types == null) return;

        foreach (var type in types)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    }

    public virtual void Receive(MessageReader reader) { }
}


public class RemoteProcess<Parameter> : RemoteProcessBase
{
    public delegate void Process(Parameter parameter, bool isCalledByMe);

    private Action<MessageWriter, Parameter> Sender { get; set; }
    private Func<MessageReader, Parameter> Receiver { get; set; }
    private Process Body { get; set; }

    public RemoteProcess(string name, Action<MessageWriter, Parameter> sender, Func<MessageReader, Parameter> receiver, RemoteProcess<Parameter>.Process process)
    : base(name)
    {
        Sender = sender;
        Receiver = receiver;
        Body = process;
    }


    public void Invoke(Parameter parameter)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Hash);
        Sender(writer, parameter);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(parameter, true);
    }

    public NebulaRPCInvoker GetInvoker(Parameter parameter)
    {
        return new NebulaRPCInvoker(Hash, (writer) => Sender(writer, parameter), () => Body.Invoke(parameter, true));
    }

    public void LocalInvoke(Parameter parameter)
    {
        Body.Invoke(parameter, true);
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(Receiver.Invoke(reader), false);
    }
}

[NebulaRPCHolder]
public class CombinedRemoteProcess : RemoteProcessBase
{
    public static CombinedRemoteProcess CombinedRPC = new();
    CombinedRemoteProcess() : base("CombinedRPC") { }

    public override void Receive(MessageReader reader)
    {
        int num = reader.ReadInt32();
        for (int i = 0; i < num; i++) RemoteProcessBase.AllNebulaProcess[reader.ReadInt32()].Receive(reader);
    }

    public void Invoke(params NebulaRPCInvoker[] invokers)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Hash);
        writer.Write(invokers.Length);
        foreach (var invoker in invokers) invoker.Invoke(writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}

public class RemoteProcess : RemoteProcessBase
{
    public delegate void Process(bool isCalledByMe);
    private Process Body { get; set; }
    public RemoteProcess(string name, Process process)
    : base(name)
    {
        Body = process;
    }

    public void Invoke()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
        writer.Write(Hash);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Body.Invoke(true);
    }

    public NebulaRPCInvoker GetInvoker()
    {
        return new NebulaRPCInvoker(Hash, (writer) => { }, () => Body.Invoke(true));
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(false);
    }
}

public class DivisibleRemoteProcess<Parameter, DividedParameter> : RemoteProcessBase
{
    public delegate void Process(DividedParameter parameter, bool isCalledByMe);

    private Func<Parameter, IEnumerator<DividedParameter>> Divider;
    private Action<MessageWriter, DividedParameter> DividedSender { get; set; }
    private Func<MessageReader, DividedParameter> Receiver { get; set; }
    private Process Body { get; set; }

    public DivisibleRemoteProcess(string name, Func<Parameter,IEnumerator<DividedParameter>> divider, Action<MessageWriter, DividedParameter> dividedSender, Func<MessageReader, DividedParameter> receiver, DivisibleRemoteProcess<Parameter, DividedParameter>.Process process)
    : base(name)
    {
        Divider = divider;
        DividedSender = dividedSender;
        Receiver = receiver;
        Body = process;
    }

    public void Invoke(Parameter parameter)
    {
        void dividedSend(DividedParameter param)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 64, Hazel.SendOption.Reliable, -1);
            writer.Write(Hash);
            DividedSender(writer, param);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            Body.Invoke(param, true);
        }
        var enumerator = Divider.Invoke(parameter);
        while (enumerator.MoveNext()) dividedSend(enumerator.Current);
    }

    public void LocalInvoke(Parameter parameter)
    {
        var enumerator = Divider.Invoke(parameter);
        while (enumerator.MoveNext()) Body.Invoke(enumerator.Current, true);
    }

    public override void Receive(MessageReader reader)
    {
        Body.Invoke(Receiver.Invoke(reader), false);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
class NebulaRPCHandlerPatch
{
    static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (callId != 64) return;

        RemoteProcessBase.AllNebulaProcess[reader.ReadInt32()].Receive(reader);
    }
}
