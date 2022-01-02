using Unity.Netcode;

namespace ExpressoBits.Inventories.Netcode
{
    [System.Serializable]
    public struct ClientRpcOptions
    {
        public bool Active;
        public bool OnlyOwner;
    }
}