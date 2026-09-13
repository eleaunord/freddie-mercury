// Synchronise l'AudioSource (ou les AudioSources) de la boîte à musique sur
// la rotation de Handle (HandleCrankRotator.IsBeingRotated).
//
// Volontairement en polling, dans Update() (et non LateUpdate) : Update() de
// TOUS les scripts s'exécute avant LateUpdate() de TOUS les scripts, donc ce
// script lit systématiquement la valeur d'IsBeingRotated calculée par
// HandleCrankRotator.LateUpdate() de la frame précédente - un décalage d'une
// frame fixe et déterministe, indépendant de tout Script Execution Order
// entre les deux composants. IsBeingRotated change potentiellement à chaque
// frame pendant une rotation : un event n'apporterait rien de plus qu'un
// polling ici.
//
// Jamais de Stop() : Pause()/UnPause() uniquement, pour préserver
// audioSource.time et reprendre le son exactement où il s'était arrêté.
//
// Plusieurs AudioSources en parallèle (au lieu d'une seule) : AudioSource
// .volume est plafonné en dur à 1.0 par le moteur (vérifié - une valeur
// supérieure est silencieusement re-clampée), donc pour un son perçu plus
// fort "à fond" sans passer par un AudioMixer, on empile plusieurs
// AudioSources identiques (même clip, même volume max) jouées en
// synchronisation parfaite - l'addition des signaux augmente le volume perçu
// (~+6dB par doublement). GetComponents (pluriel) prend toutes les
// AudioSources du GameObject, donc ajouter une deuxième AudioSource dans
// l'inspecteur (même clip "Song") suffit à profiter du cumul sans autre
// changement de code.

using UnityEngine;

namespace FreddieMercury
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicBoxAudio : MonoBehaviour
    {
        [Header("Source de rotation")]
        [Tooltip("HandleCrankRotator dont on écoute IsBeingRotated pour piloter l'AudioSource.")]
        [SerializeField] private HandleCrankRotator handle;

        [Header("Hystérésis")]
        [Tooltip("Délai sans rotation détectée avant de mettre le son en pause, en secondes. Évite que le bruit du hand tracking ne fasse clignoter play/pause frame par frame.")]
        [SerializeField] private float pauseDelay = 0.18f;

        private AudioSource[] _audioSources;
        private bool _hasEverPlayed;
        private float _timeSinceLastRotation;

        private void Awake()
        {
            _audioSources = GetComponents<AudioSource>();
            foreach (var source in _audioSources)
            {
                source.loop = true;
                source.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (handle == null) return;

            if (handle.IsBeingRotated)
            {
                _timeSinceLastRotation = 0f;

                if (!_hasEverPlayed)
                {
                    foreach (var source in _audioSources) source.Play();
                    _hasEverPlayed = true;
                }
                else
                {
                    foreach (var source in _audioSources)
                    {
                        if (!source.isPlaying) source.UnPause();
                    }
                }
            }
            else
            {
                _timeSinceLastRotation += Time.deltaTime;

                if (_hasEverPlayed && _timeSinceLastRotation >= pauseDelay)
                {
                    foreach (var source in _audioSources)
                    {
                        if (source.isPlaying) source.Pause();
                    }
                }
            }
        }
    }
}
