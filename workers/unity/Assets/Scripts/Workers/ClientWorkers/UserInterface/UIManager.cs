using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mdg.Player.Metadata;

namespace MDG.ClientSide.UserInterface
{
    public class UIManager : MonoBehaviour
    {
        public GameObject roleSelectionUI;

        public delegate void RoleSelectedHandler(PlayerType type);
        public event RoleSelectedHandler OnRoleSelected;


        public void SelectRole(string role)
        {
            Debug.LogError($"Role :{role}");
            Debug.Log(PlayerType.HUNTED.ToString());
            PlayerType type = (PlayerType) System.Enum.Parse(typeof(PlayerType), role);
            OnRoleSelected?.Invoke(type);

            roleSelectionUI.SetActive(false);
        }

    }
}