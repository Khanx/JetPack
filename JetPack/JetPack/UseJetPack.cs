using ModLoaderInterfaces;
using Shared;
using System.Collections.Generic;

namespace JetPack
{
    public class UseJetPack : IAfterWorldLoad, IOnPlayerClicked, IOnPlayerHit, IOnQuit 
    {
        private ushort jetPackID;
        private ushort fuelID;

        //AfterWorldLoad is used because the types are not defined when the class is created
        public void AfterWorldLoad()
        {
            jetPackID = ItemTypes.IndexLookup.GetIndex("Khanx.JetPack");
            fuelID = ItemTypes.IndexLookup.GetIndex("Khanx.JetPackFuel");
        }

        private static readonly int timeFlying = 300;
        private static readonly int timeParachute = timeFlying + 10;

        private readonly List<Players.PlayerID> cancelFallDamage = new List<Players.PlayerID>();

        public void OnPlayerClicked(Players.Player player, PlayerClickedData click)
        {
            if (player == null || click?.ClickType != PlayerClickedData.EClickType.Right || click?.TypeSelected != jetPackID)
                return;

            if (player.HasFlightMode)
            {
                Chatting.Chat.Send(player, "You do not need to fill the Jet Pack's tank yet.");
                return;
            }

            if (!player.Inventory.TryRemove(fuelID))
            {
                Chatting.Chat.Send(player, "The Jet Pack's tank is empty, you need Jet Pack Fuel (in inventory) to refill it.");
                return;
            }

            player.SetFlightMode(true);
            cancelFallDamage.Add(player.ID);

            Chatting.Chat.Send(player, "You are flying!!! (Press the F key)");

            ThreadManager.InvokeOnMainThread(() =>
            {
                player.SetFlightMode(false);

                Chatting.Chat.Send(player, "The Jet Pack has run out of fuel.");
            }, timeFlying);

            ThreadManager.InvokeOnMainThread(() =>
            {
                cancelFallDamage.Remove(player.ID);

                Chatting.Chat.Send(player, "The Jet Pack's parachute has stopped working.");
            }, timeParachute);
        }

        public void OnPlayerHit(Players.Player player, ModLoader.OnHitData hit)
        {
            if (player == null || hit?.HitSourceType != ModLoader.OnHitData.EHitSourceType.FallDamage || !cancelFallDamage.Contains(player.ID))
                return;

            if (hit.ResultDamage < 0)
                return;

            hit.ResultDamage = 0;

            Chatting.Chat.Send(player, "Fortunately the Jet Pack's parachute has been activated.");
        }

        public void OnQuit()
        {
            foreach(var pID in cancelFallDamage)
            {
                Players.GetPlayer(pID)?.SetFlightMode(false);
            }
        }
    }
}
