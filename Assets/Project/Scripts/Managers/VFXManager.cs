using System.Collections.Generic;
using UnityEngine;
using VContainer;


namespace NonameGame
{
    public class VFXManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem stunVfx;
        [SerializeField] private ParticleSystem salutVfx;

        [Inject] private GameModel _gameModel;

        public void PlayStunVFX(Vector3 position)
        {
            if (_gameModel.Settings.Quality != (int)QualityType.High)
                return;

            stunVfx.gameObject.SetActive(true);
            stunVfx.transform.position = position;
            stunVfx.Play();
        }
        
        public void PlaySalutVFX(Vector3 position)
        {
            if (_gameModel.Settings.Quality != (int)QualityType.High)
                return;
                
            salutVfx.gameObject.SetActive(true);
            salutVfx.transform.position = position;
            salutVfx.Play();
        }
    }
}
