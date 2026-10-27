using System.Collections.Generic;
using UnityEngine;
namespace MonsterMerge
{
    public sealed partial class MonsterVisual : MonoBehaviour
    {
        public Renderer[] bodyRenderers;
        public GameObject wings, armor, crown;
        public Transform animatedRoot;
        sealed class Joint
        {
            public Transform bone;
            public Vector3 position, scale;
            public Quaternion rotation;
            public string kind;
            public float side, phase;
        }
        readonly List<Joint> joints = new List<Joint>();
        MaterialPropertyBlock block;
        RunnerMotor motor;
        ElementForm elementForm;
        Vector3 rootPosition, rootScale, lastPosition;
        Quaternion rootRotation;
        float targetScale = 1, currentScale = 1, pulse, hit, attack, clock, gait, movement, lean, defeat, penalty;
        bool defeated, celebrating;
        float maturity, powerShape, targetPowerShape;
        Color displayedTint;
        bool tintReady;
        bool wantsFlight;
        float flightHeight, flightTarget = 1.25f;
        float flightVelocity, landing, previousHeight;
        Color tint;
        public bool IsAttacking => attack > 0;
        public bool IsDefeated => defeated;
        public int AnimatedJointCount => joints.Count;
        public float FlightHeight => flightHeight;
        public bool Flying => wantsFlight && !defeated;
        public void SetFlying(bool value, float height = 1.25f) { wantsFlight = value; flightTarget = height; }
        public void ConfigureBoss(int level, Color color)
        {
            tint=Color.Lerp(color,new Color(.18f,.13f,.22f),.48f); targetScale=.8f+Mathf.Min(level,20)*.045f;
            if (armor) armor.SetActive(level>=2); if (crown) crown.SetActive(level>=5); Paint(tint);
        }
        void Awake() { Prepare(); }
        public void Prepare()
        {
            if (block != null || !animatedRoot) return;
            block = new MaterialPropertyBlock();
            bool dragon = transform.name.ToLowerInvariant().Contains("dragon") || animatedRoot.name == "Boss rig";
            if (dragon) DragonSculpt.Build(this, animatedRoot.name == "Boss rig");
            tint = bodyRenderers != null && bodyRenderers.Length > 0 ? bodyRenderers[0].sharedMaterial.GetColor("_BaseColor") : Color.white;
            rootPosition = animatedRoot.localPosition; rootScale = animatedRoot.localScale; rootRotation = animatedRoot.localRotation;
            motor = GetComponentInParent<RunnerMotor>(); lastPosition = transform.position;
            // Element mutations should colour the limbs as well as the original torso/head list.
            var skinMaterials = new HashSet<Material>();
            if (bodyRenderers != null) foreach (var surface in bodyRenderers) if (surface) skinMaterials.Add(surface.sharedMaterial);
            var skinParts = new List<Renderer>();
            foreach (var surface in animatedRoot.GetComponentsInChildren<Renderer>(true))
                if (skinMaterials.Contains(surface.sharedMaterial)) skinParts.Add(surface);
            bodyRenderers = skinParts.ToArray();
            if (!dragon) MonsterAnatomy.Refine(animatedRoot, bodyRenderers);
            var parts = animatedRoot.GetComponentsInChildren<Transform>(true);
            Transform head = null;
            foreach (var part in parts) if (part.name == "Big head" || part.name == "Head") head = part;
            if (head)
            {
                var neck = new GameObject("Neck joint").transform;
                neck.SetParent(animatedRoot, false); neck.localPosition = head.localPosition - Vector3.up * .3f;
                foreach (var part in parts)
                    if (part == head || part.name == "Muzzle" || part.name == "Smile" || part.name == "Horn" || part.name == "Ear" || part.name == "Eye" || part.name == "Iris" || part.name == "Pupil" || part.name == "Eye glint" || part.name == "Nostril")
                        part.SetParent(neck, true);
                if (crown && !dragon) crown.transform.SetParent(neck, true);
                AddJoint(neck, "Head", 0);
            }
            foreach (var part in parts)
            {
                string kind = part.name;
                float side = Mathf.Sign(part.localPosition.x);
                if (kind == "Arm" || kind == "Fist" || kind == "Foot" || kind == "Wing" || kind == "Tail")
                {
                    var pivot = new GameObject(kind + " joint").transform;
                    pivot.SetParent(part.parent, false);
                    pivot.localPosition = part.localPosition + (!dragon && kind == "Foot" ? Vector3.up * .22f : !dragon && kind == "Arm" ? Vector3.up * .2f : Vector3.zero);
                    part.SetParent(pivot, true); AddJoint(pivot, kind, side);
                }
                else if (kind == "Eye" || kind == "Iris" || kind == "Pupil" || kind == "Eye glint" || kind == "Belly" || kind == "Torso" || kind == "Tail segment" || kind == "Jaw") AddJoint(part, kind, side);
            }
        }
        void AddJoint(Transform bone, string kind, float side)
        { joints.Add(new Joint { bone = bone, kind = kind, side = side, phase = joints.Count * .55f, position = bone.localPosition, rotation = bone.localRotation, scale = bone.localScale }); }
        public void Apply(MonsterDefinition monster, GameCatalog catalog, Element element, int stage, bool gold, int masteryStage=0)
        {
            Prepare();
            powerStyle=monster.power;
            var identity=GetComponent<DragonIdentity>();if(!identity)identity=gameObject.AddComponent<DragonIdentity>();
            if(masteryStage==0)masteryStage=GameApp.Instance?GameApp.Instance.Save.Stage(monster.id):1;
            maturity=(Mathf.Clamp(masteryStage,1,4)-1)/3f;
            if(identity.Select(this,monster,catalog.baseMaterial,masteryStage))
                foreach(var joint in joints)joint.scale=identity.RestScale(joint.bone,joint.scale);
            if(!elementForm) { elementForm=gameObject.AddComponent<ElementForm>(); elementForm.Initialize(animatedRoot,catalog.baseMaterial); }
            elementForm.Select(element);
            tint = gold ? new Color(1,.73f,.19f) : element == Element.Normal || element==monster.startingElement ? monster.color : Color.Lerp(monster.color,catalog.ElementData(element).color,.72f);
            targetScale = monster.evolutions[stage].scale;
            if (wings) wings.SetActive(true);
            if (armor) armor.SetActive(false);
            if (crown) crown.SetActive(false);
            if (!tintReady) { displayedTint=tint; tintReady=true; }
        }
        void Paint(Color color)
        {
            if (bodyRenderers == null) return;
            block.SetColor("_EmissionColor", PowerGlowColor * PowerGlow);
            block.SetColor("_BaseColor", color); block.SetFloat("_Smoothness", 0);
            foreach (var renderer in bodyRenderers) if (renderer) renderer.SetPropertyBlock(block);
        }
        public void Burst() { if (!defeated) { burstDuration=PowerDuration; pulse=burstDuration; } }
        public void Hit() { hit = .28f; }
        public void Penalty() { pulse=0; penalty = .8f; hit = .28f; }
        public void SetPower(int dna) { targetPowerShape=Mathf.Clamp01(dna/200f); targetScale *= Mathf.Lerp(.92f,1.12f,targetPowerShape); }
        public void Attack() { if (!defeated) attack = .65f; }
        public void SetDefeated(bool value) { defeated = value; celebrating = false; attack = 0; if(value)pulse=0; }
        public void Celebrate() { celebrating = true; defeated = false; }
        void Update()
        {
            if (!animatedRoot || block == null || Time.deltaTime <= 0) return;
            float dt = Time.deltaTime; clock += dt;
            previousHeight=flightHeight;
            flightHeight=Mathf.SmoothDamp(flightHeight,Flying ? flightTarget : 0,ref flightVelocity,Flying?.55f:.28f,4,dt);
            if(!Flying && previousHeight>.045f && flightHeight<=.045f) landing=1;
            landing=Mathf.Max(0,landing-dt*3.5f);
            float flight=Mathf.Clamp01(flightHeight/Mathf.Max(.1f,flightTarget));
            var velocity = (transform.position - lastPosition) / dt; lastPosition = transform.position;
            float speed = motor && motor.IsMoving ? Mathf.Clamp01(motor.Speed / 7) : 0;
            movement = Mathf.Lerp(movement, speed, 1 - Mathf.Exp(-12 * dt));
            lean = Mathf.Lerp(lean, Mathf.Clamp(-velocity.x * 3, -17, 17), 1 - Mathf.Exp(-8 * dt));
            gait += dt * Mathf.Lerp(2, 11, movement);
            pulse = Mathf.Max(0, pulse - dt); hit = Mathf.Max(0, hit - dt); attack = Mathf.Max(0, attack - dt); penalty = Mathf.Max(0,penalty-dt);
            defeat = Mathf.MoveTowards(defeat, defeated ? 1 : 0, dt * 2);
            currentScale = Mathf.Lerp(currentScale, targetScale, 1 - Mathf.Exp(-4 * dt));
            powerShape = Mathf.Lerp(powerShape,targetPowerShape,1-Mathf.Exp(-4*dt));
            displayedTint=Color.Lerp(displayedTint,tint,1-Mathf.Exp(-5*dt));
            float breath = Mathf.Sin(clock * 2.3f), stride = Mathf.Sin(gait);
            float attackPhase=1-attack/.65f;
            float strike=attack<=0?0:attackPhase<.22f ? -Mathf.Sin(attackPhase/.22f*Mathf.PI*.5f)*.32f : Mathf.Sin((attackPhase-.22f)/.78f*Mathf.PI)*Mathf.Exp(-(attackPhase-.4f)*2);
            float recoil = hit / .28f, joy = celebrating ? Mathf.Abs(Mathf.Sin(clock * 5)) : 0;
            float wingBeat=Mathf.Sin(clock*(flight>.1f?5.6f:movement>.2f?5.1f:1.8f)), squash=landing*.16f + Mathf.Sin(gait*2)*Mathf.Lerp(.045f,.025f,maturity)*movement*(1-flight) - joy*.06f;
            animatedRoot.localPosition = rootPosition + new Vector3(stride*.035f*movement*(1-flight), .025f + Mathf.Abs(stride) * Mathf.Lerp(.16f,.10f,maturity) * movement*(1-flight) + joy * Mathf.Lerp(.36f,.24f,maturity)*(1-flight) - defeat * .15f + flightHeight + wingBeat*.065f*flight - landing*.10f, strike * .48f - recoil * .18f);
            animatedRoot.localRotation = rootRotation * Quaternion.Euler(movement * Mathf.Lerp(7,15,flight) + strike * 12 - recoil * 12 + wingBeat*2*flight, breath * (1 - movement) * Mathf.Lerp(5,2.5f,maturity)-stride*2.5f*movement, lean*Mathf.Lerp(1,1.4f,flight) + stride * movement * 3*(1-flight) + defeat * 78);
            animatedRoot.localScale = Vector3.Scale(rootScale * (currentScale),new Vector3(1+squash*.5f,1-squash,1+squash*.5f));
            AnimatePowerRoot();
            float blinkPhase = clock % 4.1f;
            float eyelid = blinkPhase > 3.9f ? Mathf.Max(.06f, Mathf.Abs(blinkPhase - 4f) / .1f) : 1;
            foreach (var joint in joints)
            {
                Vector3 rotation = Vector3.zero, offset = Vector3.zero, scale = joint.scale;
                float step = Mathf.Sin(gait + (joint.side < 0 ? Mathf.PI : 0));
                switch (joint.kind)
                {
                    case "Foot": rotation.x = Mathf.Lerp(step * 37 * movement,52+wingBeat*4,flight); offset = new Vector3(0, Mathf.Pow(Mathf.Max(0, step),1.5f) * .23f * movement*(1-flight)+flight*.12f, Mathf.Cos(gait+(joint.side<0?Mathf.PI:0)) * .18f * movement*(1-flight)); break;
                    case "Arm": case "Fist": rotation = new Vector3(-step * 35 * movement - strike * 65, 0, joint.side * (breath * 4 - joy * 60)); break;
                    case "Wing":
                        scale=Vector3.Scale(scale,new Vector3(1+powerShape*.22f,1+powerShape*.12f,1));
                        float flap=Mathf.Sin(clock*(flight>.1f?5.6f:movement>.2f?5.1f:1.8f)+joint.side*.16f);
                        float downstroke=flap>0?Mathf.Pow(flap,.7f):-Mathf.Pow(-flap,1.4f);
                        float activeFlap=Mathf.Lerp(movement>.2f?22:8,38,flight);
                        rotation = new Vector3(Mathf.Sin(clock*5.6f-.5f)*8*flight, joint.side * (20 + downstroke * Mathf.Lerp(movement>.2f?22:8,42,flight)), joint.side * (flap*activeFlap - movement * 9*(1-flight)));
                        break;
                    case "Tail": scale=Vector3.Scale(scale,new Vector3(1,1,1+powerShape*.15f)); rotation = new Vector3(breath * 7, Mathf.Sin(gait - .8f) * (12 + movement * 21), Mathf.Sin(gait-1.2f)*movement*7); break;
                    case "Tail segment": rotation = new Vector3(Mathf.Sin(clock*2-joint.phase)*Mathf.Lerp(3,6,flight),Mathf.Sin(clock*3-joint.phase)*9-lean*.2f+Mathf.Sin(gait*.55f-joint.phase)*movement*7,0); break;
                    case "Jaw": rotation.x = attack > 0 ? Mathf.Sin((.65f-attack)/.65f*Mathf.PI)*32 : Mathf.Max(0,breath)*3; break;
                    case "Head": rotation = new Vector3(-movement * 6 + Mathf.Sin(gait*.5f)*movement*2 - strike * 8, Mathf.Sin(clock * .9f) * Mathf.Lerp(7,3,maturity) * (1 - movement)+Mathf.Sin(gait*.35f)*movement*3, -lean * .35f); break;
                    case "Eye": case "Iris": case "Pupil": case "Eye glint": scale.y *= eyelid; break;
                    case "Belly": case "Torso": scale = Vector3.Scale(scale, new Vector3(1 + breath * .035f, 1 + breath * .035f, 1 + breath * .035f)); break;
                }
                AnimatePowerJoint(joint.kind,joint.side,joint.phase,ref rotation);
                joint.bone.localPosition = joint.position + offset * (1 - defeat);
                joint.bone.localRotation = joint.rotation * Quaternion.Euler(rotation * (1 - defeat));
                joint.bone.localScale = scale;
            }
            Paint(penalty > 0 ? Color.Lerp(displayedTint,new Color(1,.13f,.08f),penalty*.7f) : Color.Lerp(displayedTint, Color.white, recoil * .8f));
        }
    }
}
