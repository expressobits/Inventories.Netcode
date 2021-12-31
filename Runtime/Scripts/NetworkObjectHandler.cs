using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ExpressoBits.Inventories.Netcode
{
    [RequireComponent(typeof(ItemObjectHandler))]
    public class NetworkObjectHandler : NetworkBehaviour
    {

        public ItemObjectHandler ItemObjectHandler => itemObjectHandler;

        private ItemObjectHandler itemObjectHandler;

        private void Awake()
        {
            itemObjectHandler = GetComponent<ItemObjectHandler>();
            itemObjectHandler.OnDrop += Drop;
        }

        private void Drop(ItemObject itemObject)
        {
            if (itemObject.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn(true);
            }
        }

        [ServerRpc]
        private void DropFromContainerServerRpc(NetworkBehaviourReference reference, int index, ushort amount)
        {
            if (reference.TryGet(out NetworkContainer networkContainer))
            {
                Container container = networkContainer.Container;
                if (container != null)
                {
                    itemObjectHandler.DropFromContainer(container, index, amount);
                }
            }
        }

        public void RequestDropFromContainer(NetworkContainer networkContainer, int index, ushort amount = 1)
        {
            DropFromContainerServerRpc(networkContainer, index, amount);
        }
    }
}
