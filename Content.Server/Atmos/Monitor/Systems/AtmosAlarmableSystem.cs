using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.Atmos.Monitor.Systems
{
    public class AtmosAlarmableSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosAlarmableComponent, PacketSentEvent>(OnPacketRecv);
        }

        private void OnPacketRecv(EntityUid uid, AtmosAlarmableComponent component, PacketSentEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn)
                || args.Frequency != netConn.Frequency)
                return;

            if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd)
                && cmd == AtmosMonitorSystem.AtmosMonitorAlarmCmd)
            {
                // does it have a state & network max state?
                // does it have a source?
                // and can this be alarmed by the source?
                // if so, raise an alarm
                if (args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out AtmosMonitorAlarmType state)
                    && args.Data.TryGetValue(AtmosMonitorSystem.AtmosMonitorAlarmNetMax, out AtmosMonitorAlarmType netMax)
                    && args.Data.TryGetValue(AtmosMonitorSystem.AtmosMonitorAlarmSrc, out string? source)
                    && component.AlarmedByPrototypes.Contains(source))
                    RaiseLocalEvent(component.Owner.Uid, new AtmosMonitorAlarmEvent(state, netMax));
            }
        }
    }
}
