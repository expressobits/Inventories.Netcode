using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ExpressoBits.Inventories.Netcode
{
    [RequireComponent(typeof(ItemHandler))]
    public class NetworkItemHandler : NetworkBehaviour
    {
        public NetworkContainer DefaultContainer => defaultContainer;
        public ItemHandler ItemHandler => itemHandler;

        private ItemHandler itemHandler;
        private NetworkContainer defaultContainer;
        private bool m_CachedIsServer;

        [SerializeField] private SyncRpcOptions syncPickEvent;
        [SerializeField] private SyncRpcOptions syncAddEvent;
        [SerializeField] private SyncRpcOptions syncDropEvent;
        [SerializeField] private SyncRpcOptions syncOpenEvent;
        [SerializeField] private SyncRpcOptions syncCloseEvent;

        private void Awake()
        {
            itemHandler = GetComponent<ItemHandler>();
            if (itemHandler.DefaultContainer.TryGetComponent(out NetworkContainer networkContainer))
            {
                defaultContainer = networkContainer;
            }
        }

        public override void OnNetworkSpawn()
        {
            m_CachedIsServer = IsServer;
            if (m_CachedIsServer)
            {
                itemHandler.OnDrop += OnDrop;
                itemHandler.OnAdd += OnAdd;
                itemHandler.OnPick += OnPick;
                itemHandler.OnOpen += OnOpen;
                itemHandler.OnClose += OnClose;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_CachedIsServer)
            {
                itemHandler.OnDrop -= OnDrop;
                itemHandler.OnAdd -= OnAdd;
                itemHandler.OnPick -= OnPick;
                itemHandler.OnOpen -= OnOpen;
                itemHandler.OnClose -= OnClose;
            }
        }

        #region Sync Methods
        private void OnPick(ItemObject itemObject)
        {
            if (syncPickEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncPickEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                OnPickClientRpc(itemObject.Item.ID, 1, clientRpcParams);
            }
        }

        private void OnAdd(Item item, ushort amount)
        {
            if (syncAddEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncAddEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                OnAddClientRpc(item.ID, amount, clientRpcParams);
            }
        }

        private void OnOpen(Container container)
        {
            if (!m_CachedIsServer) return;
            if (syncOpenEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncOpenEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                if (container.TryGetComponent(out NetworkContainer networkContainer))
                {
                    OnOpenContainerClientRpc(networkContainer, clientRpcParams);
                }
            }
        }

        private void OnClose(Container container)
        {
            if (!m_CachedIsServer) return;
            if (syncCloseEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncCloseEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                if (container.TryGetComponent(out NetworkContainer networkContainer))
                {
                    OnCloseContainerClientRpc(networkContainer, clientRpcParams);
                }
            }
        }

        private void OnDrop(ItemObject itemObject)
        {
            if (itemObject.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn(true);

                if (syncDropEvent.IsSync)
                {
                    ClientRpcParams clientRpcParams = default;
                    if (syncDropEvent.OnlyOwner)
                    {
                        clientRpcParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { OwnerClientId }
                            }
                        };
                    }
                    OnDropClientRpc(networkObject, clientRpcParams);
                }
            }
        }
        #endregion

        #region Server RPCs
        [ServerRpc]
        private void DropFromContainerServerRpc(NetworkBehaviourReference networkContainerReference, int index, ushort amount)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            Container container = networkContainer.Container;
            itemHandler.DropFromContainer(container, index, amount);
        }

        [ServerRpc]
        private void MoveBetweenContainersServerRpc(NetworkBehaviourReference fromNetworkContainerReference, int index, ushort amount, NetworkBehaviourReference toNetworkContainerReference)
        {
            if (!fromNetworkContainerReference.TryGet(out NetworkContainer fromNetworkContainer)) return;
            if (!toNetworkContainerReference.TryGet(out NetworkContainer toNetworkContainer)) return;

            Container fromContainer = fromNetworkContainer.Container;
            Container toContainer = toNetworkContainer.Container;

            itemHandler.MoveBetweenContainers(fromContainer, index, amount, toContainer);
        }

        [ServerRpc]
        private void SwapBetweenContainersServerRpc(NetworkBehaviourReference networkContainerReference, int index, NetworkBehaviourReference otherNetworkContainerReference, int otherIndex, ushort amount)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            if (!otherNetworkContainerReference.TryGet(out NetworkContainer otherNetworkContainer)) return;

            Container fromContainer = networkContainer.Container;
            Container otherContainer = otherNetworkContainer.Container;
            itemHandler.SwapBetweenContainers(fromContainer, index, otherContainer, otherIndex, amount);
        }

        [ServerRpc]
        private void OpenContainerServerRpc(NetworkBehaviourReference networkContainerReference)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.Open(networkContainer.Container);
        }

        [ServerRpc]
        private void OpenDefaultContainerServerRpc()
        {
            itemHandler.OpenDefaultContainer();
        }

        [ServerRpc]
        private void CloseContainerServerRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.Close(networkContainer.Container);
        }

        [ServerRpc]
        private void CloseAllContainersServerRpc()
        {
            itemHandler.CloseAllContainers();
        }
        #endregion

        #region Client Responses
        [ClientRpc]
        private void OnCloseContainerClientRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.OnClose?.Invoke(networkContainer.Container);
            itemHandler.OnCloseUnityEvent?.Invoke(networkContainer.Container);
        }

        [ClientRpc]
        private void OnOpenContainerClientRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.OnOpen?.Invoke(networkContainer.Container);
            itemHandler.OnOpenUnityEvent?.Invoke(networkContainer.Container);
        }

        [ClientRpc]
        private void OnPickClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            // TODO problem with item object without NetworkObject (Destroyed!)
            //itemHandler.OnPick?.Invoke(item, amount);
            //itemHandler.OnPickUnityEvent?.Invoke(item, amount);
        }

        [ClientRpc]
        private void OnDropClientRpc(NetworkObjectReference networkObjectReference, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            if (!networkObjectReference.TryGet(out NetworkObject networkObject)) return;
            if (!networkObject.TryGetComponent(out ItemObject itemObject)) return;
            itemHandler.OnDrop?.Invoke(itemObject);
            itemHandler.OnDropUnityEvent?.Invoke(itemObject);
        }

        [ClientRpc]
        private void OnAddClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            Item item = ItemHandler.DefaultContainer.Database.GetItem(itemId);
            itemHandler.OnAdd?.Invoke(item, amount);
            itemHandler.OnAddUnityEvent?.Invoke(item, amount);
        }
        #endregion

        #region Request From Client
        public void RequestDropFromContainer(NetworkContainer networkContainer, int index, ushort amount = 1)
        {
            DropFromContainerServerRpc(networkContainer, index, amount);
        }

        public void RequestMoveBetweenContainers(NetworkContainer fromNetworkContainer, int index, ushort amount, NetworkContainer toNetworkContainer)
        {
            MoveBetweenContainersServerRpc(fromNetworkContainer, index, amount, toNetworkContainer);
        }

        public void RequestSwapBetweenContainers(NetworkContainer networkContainer, int index, NetworkContainer otherNetworkContainer, int otherIndex, ushort amount)
        {
            SwapBetweenContainersServerRpc(networkContainer, index, otherNetworkContainer, otherIndex, amount);
        }

        public void RequestOpenDefaultContainer()
        {
            OpenDefaultContainerServerRpc();
        }

        public void RequestCloseAllContainers()
        {
            CloseAllContainersServerRpc();
        }

        public void RequestOpenContainer(NetworkContainer networkContainer)
        {
            OpenContainerServerRpc(networkContainer);
        }

        public void RequestCloseContainer(NetworkContainer networkContainer)
        {
            CloseContainerServerRpc(networkContainer);
        }
        #endregion
    }
}
