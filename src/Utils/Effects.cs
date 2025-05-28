using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalSaboteurs.src.Utils
{
    internal class Effects
    {
        public static void SetupNetwork()
        {
            IEnumerable<System.Type> types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null);
            }
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        public static List<PlayerControllerB> GetPlayers(bool includeDead = false, bool excludeOutsideFactory = false)
        {
            List<PlayerControllerB> rawList = StartOfRound.Instance.allPlayerScripts.ToList();
            List<PlayerControllerB> updatedList = new List<PlayerControllerB>(rawList);
            foreach (var p in rawList)
            {
                if (!p.IsSpawned || !p.isPlayerControlled || (!includeDead && p.isPlayerDead) || (excludeOutsideFactory && !p.isInsideFactory))
                {
                    updatedList.Remove(p);
                }
            }
            return updatedList;
        }

        public static List<EnemyAI> GetEnemies(bool includeDead = false, bool includeCanDie = false, bool excludeDaytime = false)
        {
            List<EnemyAI> rawList = Object.FindObjectsOfType<EnemyAI>().ToList();
            List<EnemyAI> updatedList = new List<EnemyAI>(rawList);
            if (includeDead)
                return updatedList;
            foreach (var e in rawList)
            {
                if (!e.IsSpawned || e.isEnemyDead || (!includeCanDie && !e.enemyType.canDie) || (excludeDaytime && e.enemyType.isDaytimeEnemy))
                {
                    updatedList.Remove(e);
                }
            }
            return updatedList;
        }

        public static void Damage(PlayerControllerB player, int damageNb, CauseOfDeath cause = 0, int animation = 0, bool criticalBlood = true)
        {
            damageNb = player.health > 100 && damageNb == 100 ? 900 : damageNb;
            if (criticalBlood && player.health - damageNb <= 20)
                player.bleedingHeavily = true;
            player.DamagePlayer(damageNb, causeOfDeath: cause, deathAnimation: animation);
        }

        public static void Heal(ulong playerID, int health)
        {
            var player = StartOfRound.Instance.allPlayerScripts[playerID];
            player.health = player.health > 100 ? player.health : health;
            player.criticallyInjured = false;
            player.bleedingHeavily = false;
            player.playerBodyAnimator.SetBool("Limp", false);
        }

        public static void Teleportation(PlayerControllerB player, Vector3 position)
        {
            player.averageVelocity = 0f;
            player.velocityLastFrame = Vector3.zero;
            player.TeleportPlayer(position, true);
            player.beamOutParticle.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
        }

        public static void SetPosFlags(ulong playerID, bool ship = false, bool exterior = false, bool interior = false)
        {
            var player = StartOfRound.Instance.allPlayerScripts[playerID];
            if (ship)
            {
                player.isInElevator = true;
                player.isInHangarShipRoom = true;
                player.isInsideFactory = false;
            }
            if (exterior)
            {
                player.isInElevator = false;
                player.isInHangarShipRoom = false;
                player.isInsideFactory = false;
            }
            if (interior)
            {
                player.isInElevator = false;
                player.isInHangarShipRoom = false;
                player.isInsideFactory = true;
            }
            foreach (var item in player.ItemSlots)
            {
                if (item != null)
                {
                    item.isInFactory = player.isInsideFactory;
                    item.isInElevator = player.isInElevator;
                    item.isInShipRoom = player.isInHangarShipRoom;
                }
            }
            if (GameNetworkManager.Instance.localPlayerController.playerClientId == player.playerClientId)
            {
                if (player.isInsideFactory)
                    TimeOfDay.Instance.DisableAllWeather();
                else
                    ActivateWeatherEffect();
            }
        }

        public static IEnumerator ShakeCameraAdvanced(ScreenShakeType shakeType, int repeat = 1, float inBetweenTimer = 0.5f)
        {
            if (shakeType == ScreenShakeType.Long || shakeType == ScreenShakeType.VeryStrong)
            {
                for (int i = 0; i < repeat; i++)
                {
                    HUDManager.Instance.playerScreenShakeAnimator.SetTrigger(shakeType == ScreenShakeType.Long ? "longShake" : "veryStrongShake");
                    if (repeat > 1)
                        yield return new WaitForSeconds(inBetweenTimer);
                }
            }
            else
                HUDManager.Instance.ShakeCamera(shakeType);
        }

        public static bool IsPlayerFacingObject<T>(PlayerControllerB player, out T obj, float distance)
        {
            if (Physics.Raycast(new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward), out var hitInfo, distance, 2816))
            {
                obj = hitInfo.transform.GetComponent<T>();
                if (obj != null)
                    return true;
            }
            obj = default;
            return false;
        }

        public static bool IsPlayerNearObject<T>(PlayerControllerB player, out T obj, float distance) where T : Component
        {
            T[] array = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (int i = 0; i < array.Length; i++)
            {
                if (Vector3.Distance(player.transform.position, array[i].transform.position) <= distance)
                {
                    obj = array[i];
                    return true;
                }
            }
            obj = default;
            return false;
        }

        public static Vector3 GetClosestAINodePosition(GameObject[] nodes, Vector3 position)
        {
            return nodes.OrderBy((GameObject x) => Vector3.Distance(position, x.transform.position)).ToArray()[0].transform.position;
        }

        public static void Knockback(Vector3 position, float range, int damage = 0, float physicsForce = 30)
        {
            Landmine.SpawnExplosion(position, false, 0, range, damage, physicsForce);
        }

        public static IEnumerator FadeOutAudio(AudioSource source, float time, bool specialStop = true)
        {
            yield return new WaitForEndOfFrame();
            var volume = source.volume;
            while (source.volume > 0)
            {
                source.volume -= volume * Time.deltaTime / time;
                if (specialStop && source.volume <= 0.04f)
                    break;
                yield return null;
            }
            source.Stop();
            source.volume = volume;
        }

        public static bool IsLocalPlayerInsideFacilityAbsolute()
        {
            if (GameNetworkManager.Instance == null)
                return false;
            var player = GameNetworkManager.Instance.localPlayerController;
            if (player == null)
                return false;
            if (!player.isPlayerDead)
                return player.isInsideFactory;
            if (player.spectatedPlayerScript == null)
                return false;
            return player.spectatedPlayerScript.isInsideFactory;
        }

        public static void ActivateWeatherEffect(LevelWeatherType originalWeather = default)
        {
            for (var i = 0; i < TimeOfDay.Instance.effects.Length; i++)
            {
                var effect = TimeOfDay.Instance.effects[i];
                var enabled = (int)StartOfRound.Instance.currentLevel.currentWeather == i;
                effect.effectEnabled = enabled;
                if (effect.effectPermanentObject != null)
                    effect.effectPermanentObject.SetActive(enabled);
                if (effect.effectObject != null)
                    effect.effectObject.SetActive(enabled);
                if (TimeOfDay.Instance.sunAnimator != null)
                {
                    if (enabled && !string.IsNullOrEmpty(effect.sunAnimatorBool))
                        TimeOfDay.Instance.sunAnimator.SetBool(effect.sunAnimatorBool, true);
                    else
                    {
                        TimeOfDay.Instance.sunAnimator.Rebind();
                        TimeOfDay.Instance.sunAnimator.Update(0);
                    }
                }
            }
            if (originalWeather == LevelWeatherType.Flooded)
            {
                var player = GameNetworkManager.Instance.localPlayerController;
                player.isUnderwater = false;
                player.sourcesCausingSinking = Mathf.Clamp(player.sourcesCausingSinking - 1, 0, 100);
                player.isMovementHindered = Mathf.Clamp(player.isMovementHindered - 1, 0, 100);
                player.hinderedMultiplier = 1f;
            }
        }

        public static void Message(string title, string bottom, bool warning = false)
        {
            HUDManager.Instance.DisplayTip(title, bottom, warning);
        }

        public static void MessageOneTime(string title, string bottom, bool warning = false, string saveKey = "LC_Tip1")
        {
            if (ES3.Load(saveKey, "LCGeneralSaveData", false))
            {
                return;
            }
            HUDManager.Instance.tipsPanelHeader.text = title;
            HUDManager.Instance.tipsPanelBody.text = bottom;
            if (warning)
            {
                HUDManager.Instance.tipsPanelAnimator.SetTrigger("TriggerWarning");
                RoundManager.PlayRandomClip(HUDManager.Instance.UIAudio, HUDManager.Instance.warningSFX, randomize: false);
            }
            else
            {
                HUDManager.Instance.tipsPanelAnimator.SetTrigger("TriggerHint");
                RoundManager.PlayRandomClip(HUDManager.Instance.UIAudio, HUDManager.Instance.tipsSFX, randomize: false);
            }
            ES3.Save(saveKey, true, "LCGeneralSaveData");
        }

        public static void MessageComputer(params string[] messages)
        {
            var dialogue = new DialogueSegment[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                dialogue[i] = new DialogueSegment
                {
                    speakerText = "PILOT COMPUTER",
                    bodyText = messages[i]
                };
            }
            HUDManager.Instance.ReadDialogue(dialogue);
        }

        public static IEnumerator Status(string text)
        {
            while (true)
            {
                HUDManager.Instance.DisplayStatusEffect(text);
                yield return new WaitForSeconds(1);
            }
        }
    }
}
