using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>Animator Controllers gerados (um por personagem/objeto animado).</summary>
    public class AnimationSet
    {
        public RuntimeAnimatorController Lumo, Slime, Bat, Thorn, Crystal, Heart, FinalCrystal, Checkpoint, Portal, Sparkle, Poof;
        public float SparkleLength, PoofLength;
    }

    /// <summary>
    /// Cria Animation Clips quadro a quadro (troca de sprite) e Animator Controllers com transições,
    /// usando a ferramenta de animação da Unity.
    /// </summary>
    public static class AnimationFactory
    {
        const string Root = "Assets/Animations/";

        public static AnimationSet CreateAll()
        {
            var set = new AnimationSet();

            // ---------------- Lumo (personagem principal)
            var idle = Clip("Player/Lumo_Idle", AssetGenerator.Frames("lumo_idle", 6), 6, true);
            var walk = Clip("Player/Lumo_Walk", AssetGenerator.Frames("lumo_walk", 6), 11, true);
            var run = Clip("Player/Lumo_Run", AssetGenerator.Frames("lumo_run", 6), 15, true);
            var jump = Clip("Player/Lumo_Jump", AssetGenerator.Frames("lumo_jump", 1), 8, true);
            var fall = Clip("Player/Lumo_Fall", AssetGenerator.Frames("lumo_fall", 1), 8, true);
            var climb = Clip("Player/Lumo_Climb", AssetGenerator.Frames("lumo_climb", 4), 9, true);
            var hurt = Clip("Player/Lumo_Hurt", AssetGenerator.Frames("lumo_hurt", 1), 8, true);
            set.Lumo = PlayerController(new[] { idle, walk, run, jump, fall, climb, hurt });

            // ---------------- Inimigos
            set.Slime = SingleState("Enemies/Slime", Clip("Enemies/Slime_Move", AssetGenerator.Frames("slime", 4), 6, true));
            set.Bat = SingleState("Enemies/Bat", Clip("Enemies/Bat_Fly", AssetGenerator.Frames("bat", 4), 12, true));
            set.Thorn = SingleState("Enemies/Thornback", Clip("Enemies/Thornback_Walk", AssetGenerator.Frames("thorn", 4), 8, true));

            // ---------------- Itens e objetos
            set.Crystal = SingleState("Items/Crystal", Clip("Items/Crystal_Spin", AssetGenerator.Frames("crystal", 6), 10, true));
            set.Heart = SingleState("Items/Heart", Clip("Items/Heart_Pulse", AssetGenerator.Frames("heart", 4), 6, true));
            set.FinalCrystal = SingleState("Items/FinalCrystal", Clip("Items/FinalCrystal_Glow", AssetGenerator.Frames("final_crystal", 6), 8, true));
            set.Portal = SingleState("Level/Portal", Clip("Level/Portal_Swirl", AssetGenerator.Frames("portal", 6), 10, true));
            set.Checkpoint = CheckpointController();

            // ---------------- Efeitos
            var sparkle = Clip("Effects/Sparkle", AssetGenerator.Frames("sparkle", 5), 16, false);
            var poof = Clip("Effects/Poof", AssetGenerator.Frames("poof", 5), 16, false);
            set.Sparkle = SingleState("Effects/Sparkle", sparkle);
            set.Poof = SingleState("Effects/Poof", poof);
            set.SparkleLength = sparkle.length;
            set.PoofLength = poof.length;
            return set;
        }

        public static AnimationClip Clip(string name, Sprite[] frames, float fps, bool loop)
        {
            string path = Root + name + ".anim";
            EditorUtil.EnsureFolder(Path.GetDirectoryName(path));
            AssetDatabase.DeleteAsset(path);

            var clip = new AnimationClip { frameRate = fps, name = Path.GetFileName(name) };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static AnimatorController NewController(string name)
        {
            string path = Root + name + ".controller";
            EditorUtil.EnsureFolder(Path.GetDirectoryName(path));
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static AnimatorController SingleState(string name, AnimationClip clip)
        {
            var controller = NewController(name);
            var sm = controller.layers[0].stateMachine;
            var state = sm.AddState(clip.name);
            state.motion = clip;
            sm.defaultState = state;
            return controller;
        }

        /// <summary>
        /// Controlador do Lumo: parâmetro inteiro "State" (0 parado, 1 andando, 2 correndo, 3 pulando,
        /// 4 caindo, 5 escalando, 6 dano) com transições a partir de Any State; "ClimbSpeed" pausa a escalada.
        /// </summary>
        static AnimatorController PlayerController(AnimationClip[] clips)
        {
            var controller = NewController("Player/Lumo");
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("ClimbSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach (var p in parameters)
                if (p.name == "ClimbSpeed") p.defaultFloat = 1f;
            controller.parameters = parameters;

            string[] names = { "Idle", "Walk", "Run", "Jump", "Fall", "Climb", "Hurt" };
            var sm = controller.layers[0].stateMachine;
            for (int i = 0; i < clips.Length; i++)
            {
                var state = sm.AddState(names[i], new Vector3(300 + (i % 2) * 220, -60 + i * 70, 0));
                state.motion = clips[i];
                if (names[i] == "Climb")
                {
                    state.speedParameterActive = true;
                    state.speedParameter = "ClimbSpeed";
                }
                var t = sm.AddAnyStateTransition(state);
                t.hasExitTime = false;
                t.duration = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.Equals, i, "State");
                if (i == 0) sm.defaultState = state;
            }
            return controller;
        }

        static AnimatorController CheckpointController()
        {
            var off = Clip("Level/Checkpoint_Off", AssetGenerator.Frames("checkpoint_off", 1), 4, true);
            var on = Clip("Level/Checkpoint_On", AssetGenerator.Frames("checkpoint_on", 4), 8, true);
            var controller = NewController("Level/Checkpoint");
            controller.AddParameter("Active", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var offState = sm.AddState("Inactive");
            offState.motion = off;
            var onState = sm.AddState("Active");
            onState.motion = on;
            sm.defaultState = offState;
            var t = offState.AddTransition(onState);
            t.hasExitTime = false;
            t.duration = 0f;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Active");
            return controller;
        }
    }
}
