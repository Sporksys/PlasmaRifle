using System.Collections.Generic;
using UnityEngine;

namespace PlasmaRifle
{
    public class TargetingController
    {
        private readonly float TargetingRange = 75f;
        
        private LinkedList<Target> targetList = new LinkedList<Target>();
        
        private LinkedListNode<Target> GetFirstNode()
        {
            if(this.targetList.Count > 0)
            {
                return this.targetList.First;
            }
            return null;
        }
        
        public void AcquireTargets()
        {
            this.targetList.Clear();
            EcoTarget[] creatures = GameObject.FindObjectsOfType<EcoTarget>();
            foreach(EcoTarget creature in creatures)
            {
                LiveMixin liveMixin = creature.gameObject.GetComponent<LiveMixin>();
                if(liveMixin != null && liveMixin.health > 0)
                {
                    string name = creature.gameObject.name;
                    if(name != null)
                    {
                        bool? isPriority = null;
                        if(possibleTargets.Contains(name))
                        {
                            isPriority = false;
                        }
                        else if(priorityTargets.Contains(name))
                        {
                            isPriority = true;
                        }
                        
                        if(isPriority != null)
                        {
                            Vector3 viewPos = MainCamera.camera.WorldToViewportPoint(creature.transform.position);
                            float distance = Vector3.Distance(creature.transform.position, MainCamera.camera.transform.position);
                            if(distance <= this.TargetingRange && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0)
                            {
                                Target target = new Target
                                {
                                    gameObject = creature.gameObject,
                                    distance = distance,
                                    isPriority = isPriority ?? false
                                };
                                
                                if(target.InitAndVerify())
                                {
                                    target.Activate();
                                    
                                    LinkedListNode<Target> first = GetFirstNode();
                                    if(first == null)
                                    {
                                        targetList.AddFirst(target);
                                    }
                                    else if(target.isPriority && (!first.Value.isPriority || target.distance < first.Value.distance))
                                    {
                                        this.targetList.AddFirst(target);
                                    }
                                    else if(!first.Value.isPriority && target.distance < first.Value.distance)
                                    {
                                        this.targetList.AddFirst(target);
                                    }
                                    else
                                    {
                                        targetList.AddAfter(targetList.First, target);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        
            if(targetList.Count > 0)
            {
                targetList.First.Value.SetActiveTarget(true);
            }
        }
        
        public void CheckForDeadTargets()
        {
            LinkedListNode<Target> node = GetFirstNode();
            while(node != null)
            {
                LinkedListNode<Target> nextNode = node.Next;
                LiveMixin liveMixin = node.Value.gameObject.GetComponent<LiveMixin>();
                if(liveMixin == null || !liveMixin.IsAlive())
                {
                    node.Value.Deactivate();
                    this.targetList.Remove(node);
                }
                node = nextNode;
            }
        }
    
        public void ReleaseTargets()
        {
            foreach(Target target in this.targetList)
            {
                target.Deactivate();
            }
            
            this.targetList.Clear();
        }
      
        public void CycleNextTarget()
        {
            LinkedListNode<Target> first = GetFirstNode();
            if(first != null)
            {
                first.Value.SetActiveTarget(false);
                
                this.targetList.RemoveFirst();
                this.targetList.AddLast(first);
                
                this.targetList.First.Value.SetActiveTarget(true);
            }
        }
    
        public GameObject GetCurrentTarget()
        {
            LinkedListNode<Target> first = GetFirstNode();
            if(first != null)
            {
                return first.Value.gameObject;
            }
            return null;
        }
    
        private readonly HashSet<string> possibleTargets = new HashSet<string>()
        {
            "Stalker(Clone)",
            "SandShark(Clone)",
            "Shocker(Clone)",
            "BoneShark(Clone)",
            "Crabsnake(Clone)",
            "CrabSquid(Clone)",
            "LavaLizard(Clone)",
            "SpineEel(Clone)",
            "Warper(Clone)"
        };
        
        private readonly HashSet<string> priorityTargets = new HashSet<string>()
        {
            "ReaperLeviathan(Clone)",
            "GhostLeviathan(Clone)",
            "GhostLeviathanJuvenile(Clone)",
            "SeaDragon(Clone)"
        };
        
        internal class Target
        {
            private readonly Color ActiveColor = new Color(2f, 0f, 0f);
            
            public GameObject gameObject;
            private SkinnedMeshRenderer mesh;
            public float distance;
            public float specInt = -1;
            public Color specColor;
            public bool isPriority;
            
            public bool InitAndVerify()
            {
                if(this.gameObject != null)
                {
                    this.mesh = this.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if(this.mesh != null && this.mesh.material != null)
                    {
                        this.specInt = this.mesh.material.GetFloat("_SpecInt");
                        this.specColor = this.mesh.material.GetColor(ShaderPropertyID._SpecColor);
                        
                        if(this.specInt != -1 && this.specColor != null)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            
            public void Deactivate()
            {
                this.mesh.material.SetFloat("_SpecInt", this.specInt);
                this.mesh.material.SetColor(ShaderPropertyID._SpecColor, specColor);
            }
            
            public void Activate()
            {
                this.mesh.material.SetFloat("_SpecInt", 50.00f);
            }
            
            public void SetActiveTarget(bool active)
            {
                if(active)
                {
                    this.mesh.material.SetColor(ShaderPropertyID._SpecColor, this.ActiveColor);
                }
                else
                {
                    this.mesh.material.SetColor(ShaderPropertyID._SpecColor, this.specColor);
                }
            }
        }
    }
}
