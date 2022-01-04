namespace ExpressoBits.Inventories.Netcode
{
    [System.Serializable]
    public struct SyncRpcOptions
    {
        public bool IsSync;
        public bool OnlyOwner;
    }
}