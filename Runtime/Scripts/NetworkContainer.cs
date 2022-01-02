using System;
using Unity.Netcode;
using UnityEngine;

namespace ExpressoBits.Inventories.Netcode
{
    [RequireComponent(typeof(Container))]
    public class NetworkContainer : NetworkBehaviour
    {
        public Container Container => container;

        private Container container;

        private NetworkList<uint> syncList;
        [SerializeField] private bool ownerWrite;
        [SerializeField] private bool triggerOpenClientRpcEvent;
        [SerializeField] private bool triggerCloseClientRpcEvent;
        [SerializeField] private bool triggerItemAddClientRpcEvent;
        [SerializeField] private bool triggerItemRemoveClientRpcEvent;

        public Action OnClientOpen;
        public Action OnClientClose;
        public Container.ItemEvent OnClientItemAdd;
        public Container.ItemEvent OnClientItemRemove;

        private void Awake()
        {
            container = GetComponent<Container>();
            syncList = new NetworkList<uint>();
            if (IsServer)
            {
                container.OnOpen += OnOpen;
                container.OnClose += OnClose;
                container.OnItemAdd += OnItemAdd;
                container.OnItemRemove += OnItemRemove;
            }
        }

        private void OnOpen()
        {
            if(triggerOpenClientRpcEvent)
            {
                OpenClientRpc();
            }
        }

        private void OnClose()
        {
            if(triggerCloseClientRpcEvent)
            {
                CloseClientRpc();
            }
        }

        private void OnItemAdd(Item item, ushort amount)
        {
            if(triggerItemAddClientRpcEvent)
            {
                ItemAddClientRpc(item.ID, amount);
            }
        }

        private void OnItemRemove(Item item, ushort amount)
        {
            if(triggerItemRemoveClientRpcEvent)
            {
                ItemRemoveClientRpc(item.ID, amount);
            }
        }

        [ClientRpc]
        private void ItemAddClientRpc(ushort itemId, ushort amount)
        {
            Item item = container.Database.GetItem(itemId);
            if(item == null) return;
            OnClientItemAdd?.Invoke(item, amount);
        }

        [ClientRpc]
        private void ItemRemoveClientRpc(ushort itemId, ushort amount)
        {
            Item item = container.Database.GetItem(itemId);
            if(item == null) return;
            OnClientItemRemove?.Invoke(item, amount);
        }

        private void Update()
        {
            if ((ownerWrite && IsOwner) || (!ownerWrite && IsServer))
            {
                for (int i = 0; i < container.Count; i++)
                {
                    Slot slot = container.ToSlot(container[i]);
                    if (syncList.Count <= i)
                    {
                        syncList.Add(slot);
                    }
                    else
                    {
                        if (syncList[i] != slot) syncList[i] = slot;
                    }
                }
                for (int i = container.Count; i < syncList.Count; i++)
                {
                    syncList.RemoveAt(i);
                    i--;
                }
            }
            else
            {
                for (int i = 0; i < syncList.Count; i++)
                {
                    Slot slot = container.ToSlot(syncList[i]);
                    if (container.Count <= i)
                    {
                        container.Add(slot);
                    }
                    else
                    {
                        if (container[i] != slot) container[i] = slot;
                    }
                }
                for (int i = syncList.Count; i < container.Count; i++)
                {
                    container.RemoveAt(i);
                    i--;
                }
            }
        }

        [ClientRpc]
        private void OpenClientRpc()
        {
            OnClientOpen?.Invoke();
        }

        [ClientRpc]
        private void CloseClientRpc()
        {
            OnClientClose?.Invoke();
        }

    }
}

