using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    public class VFXManager : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> stunVfxList;

        public void PlayStunVFX(Vector3 position)
        {
            var stunVfx = stunVfxList[0];

            //if (!stunVfx.isPlaying)
            {
                stunVfx.transform.position = position;
                stunVfx.Play();
            }
        }
    }
}
