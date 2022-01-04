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
        private NetworkVariable<bool> isOpen;
        [SerializeField] private bool ownerWrite;
        [SerializeField] private SyncRpcOptions syncItemAddEvent;
        [SerializeField] private SyncRpcOptions syncItemRemoveEvent;
        [SerializeField] private NetworkVariableReadPermission isOpenNetworkVariableReadPermission;

        private void Awake()
        {
            isOpen = new NetworkVariable<bool>(isOpenNetworkVariableReadPermission, false);
            container = GetComponent<Container>();
            syncList = new NetworkList<uint>();
            if (IsServer)
            {
                container.OnItemAdd += OnItemAdd;
                container.OnItemRemove += OnItemRemove;
            }
        }

        private void OnEnable()
        {
            if (IsServer) isOpen.Value = container.IsOpen;
            isOpen.OnValueChanged += IsOpenValueChanged;
        }

        private void OnDisable()
        {
            isOpen.OnValueChanged -= IsOpenValueChanged;
        }

        private void IsOpenValueChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                container.Open();
            }
            else
            {
                container.Close();
            }
        }

        private void OnItemAdd(Item item, ushort amount)
        {
            if (syncItemAddEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncItemAddEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                ItemAddClientRpc(item.ID, amount, clientRpcParams);
            }
        }

        private void OnItemRemove(Item item, ushort amount)
        {
            if (syncItemRemoveEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncItemRemoveEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                ItemRemoveClientRpc(item.ID, amount, clientRpcParams);
            }
        }

        [ClientRpc]
        private void ItemAddClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            if (IsServer) return;
            Item item = container.Database.GetItem(itemId);
            if (item == null) return;
            container.OnItemAdd?.Invoke(item, amount);
            container.OnItemAddUnityEvent?.Invoke(item, amount);
        }

        [ClientRpc]
        private void ItemRemoveClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            if (IsServer) return;
            Item item = container.Database.GetItem(itemId);
            if (item == null) return;
            container.OnItemRemove?.Invoke(item, amount);
            container.OnItemRemoveUnityEvent?.Invoke(item, amount);
        }

        private void Update()
        {
            if ((ownerWrite && IsOwner) || (!ownerWrite && IsServer))
            {
                if (isOpen.Value != container.IsOpen)
                {
                    isOpen.Value = container.IsOpen;
                }
            }

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

    }
}

