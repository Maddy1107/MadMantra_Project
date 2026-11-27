using System;
using System.Collections;
using UnityEngine;
using MadMantra.Player;
using MadMantra.Environment;
using MadMantra.NPC;
using MadMantra.UI;

namespace MadMantra.Cinematics
{
    public class SequenceDirector : MonoBehaviour
    {
        [Header("Pose & Timing")]
        [SerializeField] private Vector3 lyingCameraOffset = new Vector3(0f, -1.45f, 0.15f);
        [SerializeField] private Vector3 lyingHandOffset = new Vector3(-0.15f, 0.24f, -0.12f);
        [SerializeField] private float headDownPitch = 40f;
        [SerializeField] private float headUpPitch = 0f;
        [SerializeField] private float headLiftDuration = 1.6f;
        [SerializeField] private float reactionPause = 0.6f;
        [SerializeField] private float getUpDuration = 4.5f;
        [SerializeField] private float settleDuration = 0.8f;
        [SerializeField] private float flickerDuration = 8f;

        [Header("Intro NPC & Door")]
        [SerializeField] private Door securityDoor;
        [SerializeField] private NPCController introNpc;
        [SerializeField] private Vector3 npcIntroStartPos = new Vector3(0f, 0f, 3.6f);
        [SerializeField] private Vector3 npcThroughDoorPos = new Vector3(0f, 0f, 6.2f);
        [SerializeField] private float npcWalkSpeed = 2.0f;

        [Header("Debug & References")]
        [SerializeField] private bool skipWakeUp = false;
        [SerializeField] private PlayerController player;
        [SerializeField] private EyelidOverlay eyelids;
        [SerializeField] private LightFlicker[] flickerLights;

        private void Start()
        {
            if (player == null) return;
            if (skipWakeUp) { eyelids?.SetOpenness(1f); EnableGameplay(); return; }
            StartCoroutine(WakeUpRoutine());
        }

        private IEnumerator WakeUpRoutine()
        {
            player.CanMove = player.CanLook = false;

            Transform mount = player.CameraMount;
            Vector3 standPos = mount.localPosition, liePos = standPos + lyingCameraOffset;
            Quaternion standRot = mount.localRotation;
            Quaternion headDown = Quaternion.Euler(headDownPitch, 0f, 0f);
            Quaternion headUp = Quaternion.Euler(headUpPitch, 0f, 0f);

            // Place NPC inside Room 1 facing the door; remember his Room 2 spot
            Vector3 room2Pos = introNpc != null ? introNpc.transform.position : Vector3.zero;
            Quaternion room2Rot = introNpc != null ? introNpc.transform.rotation : Quaternion.identity;
            if (introNpc != null) introNpc.transform.SetPositionAndRotation(npcIntroStartPos, Quaternion.identity);

            // Start face-down on floor with flickering lights
            mount.SetLocalPositionAndRotation(liePos, headDown);
            player.Hand?.SetPoseOffset(lyingHandOffset);
            foreach (var fl in flickerLights) fl?.Flicker(flickerDuration);

            // 1. Blink awake
            if (eyelids != null)
            {
                eyelids.SetOpenness(0f);
                yield return new WaitForSeconds(1.0f);
                yield return LerpOver(0.7f, s => eyelids.SetOpenness(Mathf.Lerp(0f, 0.4f, s)));
                yield return LerpOver(0.3f, s => eyelids.SetOpenness(Mathf.Lerp(0.4f, 0f, s)));
                yield return new WaitForSeconds(0.4f);
                yield return LerpOver(0.8f, s => eyelids.SetOpenness(s));
            }

            // 2. Lift head
            yield return LerpOver(headLiftDuration, s => mount.localRotation = Quaternion.Slerp(headDown, headUp, s));

            // 3. NPC opens door, walks into Room 2, closes door
            yield return NpcLeaveThroughDoor(room2Pos, room2Rot);
            yield return new WaitForSeconds(reactionPause);

            // 4. Stand up
            yield return LerpOver(getUpDuration, s =>
            {
                mount.SetLocalPositionAndRotation(Vector3.Lerp(liePos, standPos, s), Quaternion.Slerp(headUp, standRot, s));
                player.Hand?.SetPoseOffset(Vector3.Lerp(lyingHandOffset, Vector3.zero, s));
            });

            yield return new WaitForSeconds(settleDuration);
            EnableGameplay();
        }

        private IEnumerator NpcLeaveThroughDoor(Vector3 room2Pos, Quaternion room2Rot)
        {
            bool wasLocked = securityDoor != null && securityDoor.IsLocked;
            securityDoor?.OpenDoor();
            yield return new WaitForSeconds(0.45f);

            if (introNpc != null) yield return introNpc.WalkTo(npcThroughDoorPos, npcWalkSpeed);

            if (securityDoor != null) { securityDoor.CloseDoor(); if (wasLocked) securityDoor.IsLocked = true; }

            if (introNpc != null)
            {
                yield return introNpc.WalkTo(room2Pos, npcWalkSpeed);
                yield return introNpc.TurnTo(room2Rot);
            }
        }

        private IEnumerator LerpOver(float duration, Action<float> apply)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                apply(Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            apply(1f);
        }

        private void EnableGameplay()
        {
            player.CanMove = player.CanLook = true;
            UIManager.Instance?.SetCrosshair(true);
            UIManager.Instance?.SetObjective("Inspect the room and access the control panel.");
        }
    }
}
