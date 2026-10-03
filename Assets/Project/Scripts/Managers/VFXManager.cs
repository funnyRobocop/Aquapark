using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    public class VFXManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem stunVfx;
        [SerializeField] private ParticleSystem salutVfx;

        public void PlayStunVFX(Vector3 position)
        {
            stunVfx.gameObject.SetActive(true);
            stunVfx.transform.position = position;
            stunVfx.Play();
        }
        
        public void PlaySalutVFX(Vector3 position)
        {
            salutVfx.gameObject.SetActive(true);
            salutVfx.transform.position = position;
            salutVfx.Play();
        }
    }
}
