using Fusion;
using UnityEngine;

namespace NonameGame
{
    public static class PlayerLookup
    {
        public static PlayerGrab FindGrab(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return null;

            var obj = runner.GetPlayerObject(player);
            if (obj == null)
                return null;

            return obj.GetComponent<PlayerGrab>() ?? obj.GetComponentInChildren<PlayerGrab>();
        }
    }
}
