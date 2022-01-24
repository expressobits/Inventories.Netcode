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
        [SerializeField] private SyncRpcOptions syncItemAddEvent;
        [SerializeField] private SyncRpcOptions syncItemRemoveEvent;
        [SerializeField] private NetworkVariableReadPermission isOpenNetworkVariableReadPermission;

        private void Awake()
        {
            isOpen = new NetworkVariable<bool>(isOpenNetworkVariableReadPermission, false);
            container = GetComponent<Container>();
            syncList = new NetworkList<uint>();
        }

        private void OnEnable()
        {
            if (IsServer) isOpen.Value = container.IsOpen;
            isOpen.OnValueChanged += IsOpenValueChanged;
            syncList.OnListChanged += ListChanged;
            if (IsServer)
            {
                container.OnItemAdd += OnItemAdd;
                container.OnItemRemove += OnItemRemove;
                container.OnAdd += Add;
                container.OnRemoveAt += RemoveAt;
                container.OnUpdate += UpdateSlot;
            }
        }

        private void ListChanged(NetworkListEvent<uint> changeEvent)
        {
            if(IsServer) return;
            Slot slot;
            switch (changeEvent.Type)
            {
                case NetworkListEvent<uint>.EventType.Add:
                    slot = container.ToSlot(changeEvent.Value);
                    container.Add(slot);
                    break;
                case NetworkListEvent<uint>.EventType.RemoveAt:
                    container.RemoveAt(changeEvent.Index);
                    break;
                case NetworkListEvent<uint>.EventType.Remove:
                    slot = container.ToSlot(changeEvent.Value);
                    container.Remove(slot);
                    break;
                case NetworkListEvent<uint>.EventType.Insert:
                    slot = container.ToSlot(changeEvent.Value);
                    container.Remove(slot);
                    break;
                case NetworkListEvent<uint>.EventType.Value:
                    slot = container.ToSlot(changeEvent.Value);
                    container[changeEvent.Index] = slot;
                    break;
            }
        }

        private void OnDisable()
        {
            isOpen.OnValueChanged -= IsOpenValueChanged;
            syncList.OnListChanged -= ListChanged;
            if (IsServer)
            {
                container.OnItemAdd -= OnItemAdd;
                container.OnItemRemove -= OnItemRemove;
                container.OnAdd -= Add;
                container.OnRemoveAt -= RemoveAt;
                container.OnUpdate -= UpdateSlot;
            }
        }

        private void Add(Slot slot)
        {
            syncList.Add(slot);
        }

        private void RemoveAt(int index)
        {
            syncList.RemoveAt(index);
        }

        private void UpdateSlot(int index)
        {
            syncList[index] = container[index];
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

        #region Sync Events
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
        #endregion

        #region Client Responses
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
        #endregion

        private void Update()
        {
            if (IsServer)
            {
                if (isOpen.Value != container.IsOpen)
                {
                    isOpen.Value = container.IsOpen;
                }
            }

            if (IsServer)
            {
                // for (int i = 0; i < container.Count; i++)
                // {
                //     Slot slot = container.ToSlot(container[i]);
                //     if (syncList.Count <= i)
                //     {
                //         syncList.Add(slot);
                //     }
                //     else
                //     {
                //         if (syncList[i] != slot) syncList[i] = slot;
                //     }
                // }
                // for (int i = container.Count; i < syncList.Count; i++)
                // {
                //     syncList.RemoveAt(i);
                //     i--;
                // }
            }
            else
            {
                // Make with network events
            }
        }

    }
}

