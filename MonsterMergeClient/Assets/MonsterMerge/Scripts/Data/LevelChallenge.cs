using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMerge
{
    public static class LevelChallenge
    {
        static readonly string[] Names =
        {
            "COIN WEAVE", "SPIKE SLALOM", "METEOR DODGE", "ROTOR RUN",
            "MUTATION GATES", "FIRE CROSSING", "MOVING WALLS", "POWER PICKUPS",
            "ROCKFALL", "STORM COURSE"
        };
        static readonly string[] Tips =
        {
            "Weave across the lanes to follow the coin trail.",
            "Steer around the spike rows and collect the DNA nearby.",
            "Watch the warning rings and move clear of falling meteors.",
            "Time your crossing past the sweeping rotor bars.",
            "Choose the mutation gate that matches your dragon.",
            "Cross the fire lanes carefully and keep moving.",
            "Watch the moving walls and change lanes at the right time.",
            "Follow the shifting DNA rows to build boss power.",
            "Dodge the rockfall, then line up for the next pickup.",
            "Combine quick lane changes with careful timing."
        };

        public static string Name(int level) => "LEVEL "+Mathf.Max(1,level)+" / "+Names[(Mathf.Max(1, level) - 1) % Names.Length];
        public static string Tip(int level) => Tips[(Mathf.Max(1, level) - 1) % Tips.Length];

        public static LevelDefinition CreateRuntime(LevelDefinition source, int levelNumber)
        {
            var runtime = ScriptableObject.CreateInstance<LevelDefinition>();
            runtime.name = source.name + " / Level " + levelNumber;
            runtime.number = levelNumber;
            runtime.length = source.length;
            runtime.completionCoins = source.completionCoins;
            runtime.boss = source.boss;
            runtime.bossMultiplier = source.bossMultiplier;
            var segments = new List<SegmentDefinition>();
            if(source.segments!=null)
                foreach (var item in source.segments)
                    if (item != null) segments.Add(new SegmentDefinition { kind=item.kind, z=item.z, lane=item.lane, left=item.left, right=item.right });
            AddChallenge(segments, levelNumber, source.length);
            runtime.segments = segments.ToArray();
            TrackLayout.Space(runtime);
            return runtime;
        }

        static void AddChallenge(List<SegmentDefinition> segments, int level, float length)
        {
            int type=(Mathf.Max(1,level)-1)%Names.Length;
            var random=new System.Random(unchecked(level*7919+104729));
            float start=Mathf.Max(25,length*.22f);
            float step=Mathf.Max(15,length*.085f);
            float variation=(float)random.NextDouble()*2.5f;
            for(int i=0;i<4;i++)
            {
                float z=start+i*step+variation;
                float lane=(random.Next(0,3)-1)*1.65f;
                switch(type)
                {
                    case 0:
                        segments.Add(new SegmentDefinition{kind=SegmentKind.Coins,z=z,lane=lane});
                        break;
                    case 1:
                        segments.Add(new SegmentDefinition{kind=i%2==0?SegmentKind.Spikes:SegmentKind.Dna,z=z,lane=lane});
                        break;
                    case 2:
                        segments.Add(new SegmentDefinition{kind=i%2==0?SegmentKind.FallingObject:SegmentKind.Coins,z=z,lane=lane});
                        break;
                    case 3:
                        segments.Add(new SegmentDefinition{kind=SegmentKind.RotatingBar,z=z,lane=0});
                        break;
                    case 4:
                        segments.Add(new SegmentDefinition{kind=SegmentKind.Gate,z=z,lane=0,left=(Element)(i%4),right=(Element)((i+1)%4)});
                        break;
                    case 5:
                        segments.Add(new SegmentDefinition{kind=SegmentKind.FireZone,z=z,lane=lane});
                        break;
                    case 6:
                        segments.Add(new SegmentDefinition{kind=i%2==0?SegmentKind.MovingBlock:SegmentKind.Dna,z=z,lane=lane});
                        break;
                    case 7:
                        segments.Add(new SegmentDefinition{kind=SegmentKind.Choice,z=z,lane=lane});
                        break;
                    case 8:
                        segments.Add(new SegmentDefinition{kind=i%2==0?SegmentKind.FallingObject:SegmentKind.MovingBlock,z=z,lane=lane});
                        break;
                    default:
                        segments.Add(new SegmentDefinition{kind=i%2==0?SegmentKind.FireZone:SegmentKind.RotatingBar,z=z,lane=i%2==0?lane:0});
                        break;
                }
            }
        }
    }
}
