using Unity.Netcode;
using UnityEngine;

namespace ExpressoBits.Inventories.Netcode
{
    [RequireComponent(typeof(Crafter))]
    public class NetworkCrafter : NetworkBehaviour
    {
        private Crafter crafter;

        public Crafter Crafter => crafter;

        [SerializeField] private SyncRpcOptions syncRequestCraftEvent;
        [SerializeField] private SyncRpcOptions syncCraftedEvent;
        [SerializeField] private SyncRpcOptions syncAddCraftingEvent;
        [SerializeField] private SyncRpcOptions syncRemoveCraftingAtEvent;
        private NetworkList<Crafting> syncCraftings;
        private bool m_CachedIsServer;

        private void Awake()
        {
            crafter = GetComponent<Crafter>();
            syncCraftings = new NetworkList<Crafting>();
        }

        public override void OnNetworkSpawn()
        {
            m_CachedIsServer = IsServer;
            if (m_CachedIsServer)
            {
                crafter.OnRequestCraft += OnRequestCraft;
                crafter.OnCrafted += OnCrafted;
                crafter.OnAdd += AddCraft;
                crafter.OnRemoveAt += RemoveAtCraft;
                crafter.OnUpdate += UpdateCraft;
            }
            else
            {
                crafter.SetCanFinishCraft(false);
            }
            syncCraftings.OnListChanged += ListChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (m_CachedIsServer)
            {
                crafter.OnRequestCraft -= OnRequestCraft;
                crafter.OnCrafted -= OnCrafted;
                crafter.OnAdd -= AddCraft;
                crafter.OnRemoveAt -= RemoveAtCraft;
                crafter.OnUpdate -= UpdateCraft;
            }
            syncCraftings.OnListChanged -= ListChanged;
        }

        private void ListChanged(NetworkListEvent<Crafting> changeEvent)
        {
            if(m_CachedIsServer) return;
            switch (changeEvent.Type)
            {
                case NetworkListEvent<Crafting>.EventType.Add:
                    crafter.Add(changeEvent.Value);
                    break;
                case NetworkListEvent<Crafting>.EventType.RemoveAt:
                    crafter.RemoveAt(changeEvent.Index);
                    break;
                case NetworkListEvent<Crafting>.EventType.Value:
                    //crafter[changeEvent.Index] = changeEvent.Value;
                    break;
            }
        }

        private void AddCraft(Crafting crafting)
        {
            syncCraftings.Add(crafting);
        }

        private void RemoveAtCraft(int index)
        {
            syncCraftings.RemoveAt(index);
        }

        private void UpdateCraft(int index)
        {
            syncCraftings[index] = crafter[index];
        }

        #region Sync Events
        private void OnRequestCraft(Recipe recipe)
        {
            int indexOfRecipe = Crafter.Recipes.IndexOf(recipe);
            if (indexOfRecipe < 0) return;
            if (syncRequestCraftEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncRequestCraftEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                OnRequestCraftClientRpc(indexOfRecipe, clientRpcParams);
            }
        }

        private void OnCrafted(Recipe recipe)
        {
            int indexOfRecipe = Crafter.Recipes.IndexOf(recipe);
            if (indexOfRecipe < 0) return;
            if (syncCraftedEvent.IsSync)
            {
                ClientRpcParams clientRpcParams = default;
                if (syncCraftedEvent.OnlyOwner)
                {
                    clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                OnCraftedClientRpc(indexOfRecipe, clientRpcParams);
            }
        }
        #endregion

        #region Client Responses
        [ClientRpc]
        private void OnRequestCraftClientRpc(int indexOfRecipe, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            if (crafter.Recipes.Count <= indexOfRecipe) return;
            Recipe recipe = crafter.Recipes[indexOfRecipe];
            crafter.OnRequestCraft?.Invoke(recipe);
        }

        [ClientRpc]
        private void OnCraftedClientRpc(int indexOfRecipe, ClientRpcParams clientRpcParams = default)
        {
            if (m_CachedIsServer) return;
            if (crafter.Recipes.Count <= indexOfRecipe) return;
            Recipe recipe = crafter.Recipes[indexOfRecipe];
            crafter.OnCrafted?.Invoke(recipe);
        }
        #endregion

        [ServerRpc]
        private void CraftServerRpc(int indexOfRecipe)
        {
            if (crafter.Database.Recipes.Count <= indexOfRecipe) return;
            Recipe recipe = crafter.Database.Recipes[indexOfRecipe];
            crafter.Craft(recipe);
        }

        [ServerRpc]
        private void CancelCraftServerRpc(int indexOfCrafting)
        {
            crafter.CancelCraft(indexOfCrafting);
        }

        public void RequestCraft(Recipe recipe)
        {
            int indexOfRecipe = crafter.Database.Recipes.IndexOf(recipe);
            if (indexOfRecipe < 0) return;
            CraftServerRpc(indexOfRecipe);
        }

        public void RequestCancelCraft(Crafting crafting)
        {
            int indexOfCrafting = Crafter.IndexOf(crafting);
            CancelCraftServerRpc(indexOfCrafting);
        }
    }
}

