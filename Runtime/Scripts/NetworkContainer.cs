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

        private void Awake()
        {
            container = GetComponent<Container>();
            syncList = new NetworkList<uint>();
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

    }
}

