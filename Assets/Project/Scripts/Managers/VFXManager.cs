using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    public class VFXManager : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> stunVfxList;
        [SerializeField] private List<ParticleSystem> salutVfxList;

        public void PlayStunVFX(Vector3 position, bool isBig = false)
        {
            var stunVfx = isBig ? stunVfxList[1] : stunVfxList[0];

            stunVfx.gameObject.SetActive(true);
            stunVfx.transform.position = position;
            stunVfx.Play();
        }
        public void PlaySalutVFX(Vector3 position, bool isBig = false)
        {
            var salutVfx = isBig ? salutVfxList[1] : salutVfxList[0];

            salutVfx.gameObject.SetActive(true);
            salutVfx.transform.position = position;
            salutVfx.Play();
        }
    }
}
