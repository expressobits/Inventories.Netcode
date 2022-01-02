using System;
using Unity.Netcode;
using UnityEngine;

namespace ExpressoBits.Inventories.Netcode
{
    [RequireComponent(typeof(ItemHandler))]
    public class NetworkItemHandler : NetworkBehaviour
    {
        public NetworkContainer DefaultContainer => defaultContainer;
        public ItemHandler ItemHandler => itemHandler;

        public Action<NetworkContainer> OnClientOpen;
        public Action<NetworkContainer> OnClientClose;
        public Container.ItemEvent OnClientPick;
        public Container.ItemEvent OnClientAdd;
        public ItemHandler.ItemObjectEvent OnClientDrop;

        private ItemHandler itemHandler;
        private NetworkContainer defaultContainer;

        [SerializeField] private ClientRpcOptions triggerPickClientRpc;
        [SerializeField] private ClientRpcOptions triggerAddClientRpc;
        [SerializeField] private ClientRpcOptions triggerDropClientRpc;
        [SerializeField] private ClientRpcOptions triggerOpenClientRpc;
        [SerializeField] private ClientRpcOptions triggerCloseClientRpc;

        private void Awake()
        {
            itemHandler = GetComponent<ItemHandler>();
            if (itemHandler.DefaultContainer.TryGetComponent(out NetworkContainer networkContainer))
            {
                defaultContainer = networkContainer;
            }
            itemHandler.OnDrop += OnDrop;
            itemHandler.OnAdd += OnAdd;
            itemHandler.OnPick += OnPick;
            itemHandler.OnOpen += OnOpen;
            itemHandler.OnClose += OnClose;
        }

        private void OnPick(ItemObject itemObject)
        {
            if (triggerPickClientRpc.Active)
            {
                ClientRpcParams clientRpcParams = default;
                if (triggerPickClientRpc.OnlyOwner)
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
            if (triggerAddClientRpc.Active)
            {
                ClientRpcParams clientRpcParams = default;
                if (triggerAddClientRpc.OnlyOwner)
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
            if (triggerOpenClientRpc.Active)
            {
                ClientRpcParams clientRpcParams = default;
                if (triggerOpenClientRpc.OnlyOwner)
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
            if (triggerCloseClientRpc.Active)
            {
                ClientRpcParams clientRpcParams = default;
                if (triggerCloseClientRpc.OnlyOwner)
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

                if (triggerDropClientRpc.Active)
                {
                    ClientRpcParams clientRpcParams = default;
                    if (triggerDropClientRpc.OnlyOwner)
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

        [ServerRpc]
        private void DropFromContainerServerRpc(NetworkBehaviourReference networkContainerReference, int index, ushort amount)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            Container container = networkContainer.Container;
            itemHandler.DropFromContainer(container, index, amount);
        }

        [ServerRpc]
        private void SwapBetweenContainersServerRpc(NetworkBehaviourReference fromNetworkContainerReference, int index, ushort amount, NetworkBehaviourReference toNetworkContainerReference)
        {
            if (!fromNetworkContainerReference.TryGet(out NetworkContainer fromNetworkContainer)) return;
            if (!toNetworkContainerReference.TryGet(out NetworkContainer toNetworkContainer)) return;

            Container fromContainer = fromNetworkContainer.Container;
            Container toContainer = toNetworkContainer.Container;

            itemHandler.SwapBetweenContainers(fromContainer, index, amount, toContainer);
        }

        [ServerRpc]
        private void OpenContainerServerRpc(NetworkBehaviourReference networkContainerReference)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.Open(networkContainer.Container);
        }

        [ServerRpc]
        private void CloseContainerServerRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            itemHandler.Close(networkContainer.Container);
        }

        [ClientRpc]
        private void OnCloseContainerClientRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            OnClientClose?.Invoke(networkContainer);
        }

        [ClientRpc]
        private void OnOpenContainerClientRpc(NetworkBehaviourReference networkContainerReference, ClientRpcParams clientRpcParams = default)
        {
            if (!networkContainerReference.TryGet(out NetworkContainer networkContainer)) return;
            OnClientOpen?.Invoke(networkContainer);
        }

        [ClientRpc]
        private void OnPickClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            Item item = ItemHandler.DefaultContainer.Database.GetItem(itemId);
            OnClientPick?.Invoke(item, amount);
        }

        [ClientRpc]
        private void OnDropClientRpc(NetworkObjectReference networkObjectReference, ClientRpcParams clientRpcParams = default)
        {
            if(!networkObjectReference.TryGet(out NetworkObject networkObject)) return;
            if(!networkObject.TryGetComponent(out ItemObject itemObject)) return;
            OnClientDrop?.Invoke(itemObject);
        }

        [ClientRpc]
        private void OnAddClientRpc(ushort itemId, ushort amount, ClientRpcParams clientRpcParams = default)
        {
            Item item = ItemHandler.DefaultContainer.Database.GetItem(itemId);
            OnClientAdd?.Invoke(item, amount);
        }

        public void RequestDropFromContainer(NetworkContainer networkContainer, int index, ushort amount = 1)
        {
            DropFromContainerServerRpc(networkContainer, index, amount);
        }

        public void RequestSwapBetweenContainers(NetworkContainer fromNetworkContainer, int index, ushort amount, NetworkContainer toNetworkContainer)
        {
            SwapBetweenContainersServerRpc(fromNetworkContainer, index, amount, toNetworkContainer);
        }

        public void RequestOpenContainer(NetworkContainer networkContainer)
        {
            OpenContainerServerRpc(networkContainer);
        }

        public void RequestCloseContainer(NetworkContainer networkContainer)
        {
            CloseContainerServerRpc(networkContainer);
        }
    }
}
