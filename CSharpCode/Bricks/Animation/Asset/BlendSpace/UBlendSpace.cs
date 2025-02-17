﻿using EngineNS.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EngineNS.Animation.Asset.BlendSpace
{
    public delegate void AxisNameChange(string newName);
    [Rtti.Meta]
    public class UBlendSpace_Axis : IO.BaseSerializer
    {
        public event AxisNameChange OnAxisNameChange;
        string mAxisName = "None";
        [Rtti.Meta]
        public string AxisName
        {
            get => mAxisName;
            set
            {
                mAxisName = value;
                OnAxisNameChange?.Invoke(value);
            }
        }
        [Rtti.Meta]
        public float Min { get; set; }
        [Rtti.Meta]
        public float Max { get; set; }
        [Rtti.Meta]
        public int GridNum { get; set; }
        public UBlendSpace_Axis()
        {

        }
        public UBlendSpace_Axis(string axisName = "None", float min = 0.0f, float max = 100.0f, int gridNum = 4)
        {
            AxisName = axisName;
            Min = min;
            Max = max;
            GridNum = gridNum;
        }
        public float Range
        {
            get { return Max - Min; }
        }
        public float GridSize
        {
            get { return Range / (float)GridNum; }
        }
    }
    public struct BlendSample
    {
        public int AnimationIndex;
        public IAnimationAsset Animation;
        public float TotalWeight;
        public float Time;
        public float PreviousTime;
        public float SamplePlayRate;

        //FMarkerTickRecord MarkerTickRecord;
        //List<float> PerBoneBlendData;
    }
    //BlendSpace中 动作和所在的位置
    [Rtti.Meta]
    public class UBlendSpace_Point : IO.BaseSerializer
    {
        public event EventHandler OnAnimationChanged;
        [Rtti.Meta]
        public Vector3 Value { get; set; }
        [Rtti.Meta]
        public IAnimationAsset Animation
        {
            get;
            set;
        }
        public UBlendSpace_Point(IAnimationAsset animation, Vector3 value)
        {
            Animation = animation;
            Value = value;
        }
        public UBlendSpace_Point()
        {
        }
    }
    [Rtti.Meta]
    public class UBlentSpace_Triangle : IO.BaseSerializer
    {
        public static int MaxVertices = 3;
        [Rtti.Meta]
        public List<int> Indices { get; set; } = new List<int>() { -1, -1, -1 };
        [Rtti.Meta]
        public List<float> Weights { get; set; } = new List<float>() { 0, 0, 0 };
        public UBlentSpace_Triangle()
        {

        }
        public UBlentSpace_Triangle(int count)
        {
            Indices = new List<int>();
            Weights = new List<float>();
            for (int i = 0; i < count; ++i)
            {
                Indices.Add(-1);
                Weights.Add(0);
            }
        }
    }
    //BlendSpace Grid里每个元素和权重
    public struct WeightedTriangle
    {
        public UBlentSpace_Triangle Triangle;
        public float BlendWeight;
    }

    //need to seperate editor and runtime
    [Rtti.Meta]
    public abstract class UBlendSpace : IO.BaseSerializer, IAnimationCompositeAsset
    {
        [Rtti.Meta]
        public List<UBlendSpace_Axis> BlendAxises = new List<UBlendSpace_Axis>() { null, null, null };
        [Rtti.Meta]
        public List<UBlentSpace_Triangle> GridTriangles { get; protected set; } = new List<UBlentSpace_Triangle>();
 
        [Rtti.Meta]    
        public List<UBlendSpace_Point> AnimPoints { get; protected set; } = new List<UBlendSpace_Point>();

        public UBlendSpace_Point GetAnimPoint(RName name)
        {
           var temp = AnimPoints.Find((point) => { return point.Animation.AssetName == name; });
            return temp;
        }
        public UBlendSpace_Point GetAnimPoint(int index)
        {
            if (AnimPoints.Count < index)
                return null;
            return AnimPoints[index];
        }

        protected UBlendSpace()
        {

        }
    
        //add to blendSpace and copy Clone Pose
        public virtual UBlendSpace_Point AddPoint(IAnimationAsset animationAsset, Vector3 value)
        {
            //valid sample
            var animPoint = new UBlendSpace_Point(animationAsset, value);
            AnimPoints.Add(animPoint);
            animPoint.OnAnimationChanged += AnimPoint_OnAnimationChanged;
            ReConstructTrangles();
            return animPoint;
        }

        private void AnimPoint_OnAnimationChanged(object sender, EventArgs e)
        {
            //StretchAnimationClips();
        }


        protected virtual void ReConstructTrangles()
        {

        }
        public bool RemovePoint(int index)
        {
            if (AnimPoints.Count < index)
                return false;
            AnimPoints[index].OnAnimationChanged -= AnimPoint_OnAnimationChanged;
            AnimPoints.RemoveAt(index);
            ReConstructTrangles();
            return true;
        }
        public void ReFresh()
        {
            ReConstructTrangles();
        }
        protected virtual void ExtractWeightedTrianglesByInput(Vector3 input, List<WeightedTriangle> gridElementSamples)
        {
            return;
        }
        protected Vector3 GriddingInput(Vector3 input)
        {
            Vector3 MinBlendInput = new Vector3(BlendAxises[0].Min, BlendAxises[1].Min, BlendAxises[2].Min);
            Vector3 MaxBlendInput = new Vector3(BlendAxises[0].Max, BlendAxises[1].Max, BlendAxises[2].Max);
            Vector3 GridSize = new Vector3(BlendAxises[0].GridSize, BlendAxises[1].GridSize, BlendAxises[2].GridSize);

            Vector3 griddingInput;
            griddingInput.X = MathHelper.Clamp(input.X, MinBlendInput.X, MaxBlendInput.X);
            griddingInput.Y = MathHelper.Clamp<float>(input.Y, MinBlendInput.Y, MaxBlendInput.Y);
            griddingInput.Z = MathHelper.Clamp<float>(input.Z, MinBlendInput.Z, MaxBlendInput.Z);

            griddingInput -= MinBlendInput;
            griddingInput.X /= GridSize.X;
            griddingInput.Y /= GridSize.Y;
            griddingInput.Z /= GridSize.Z;
            return griddingInput;
        }
        protected void FillTheGrid(int[] sortedIndex2SampleIndex, List<UBlentSpace_Triangle> gridElements)
        {
            GridTriangles.Clear();
            for (int i = 0; i < gridElements.Count; ++i)
            {
                var element = gridElements[i];
                var newElement = new UBlentSpace_Triangle(3);
                float totalWeight = 0;
                for (int vertexIndex = 0; vertexIndex < UBlentSpace_Triangle.MaxVertices; ++vertexIndex)
                {
                    int sortedIndex = element.Indices[vertexIndex];
                    if (sortedIndex != -1 && sortedIndex2SampleIndex[sortedIndex] != -1)
                    {
                        newElement.Indices[vertexIndex] = sortedIndex2SampleIndex[sortedIndex];
                    }
                    else
                    {
                        newElement.Indices[vertexIndex] = -1;
                    }

                    if (newElement.Indices[vertexIndex] == -1)
                    {
                        newElement.Weights[vertexIndex] = 0.0f;
                    }
                    else
                    {
                        newElement.Weights[vertexIndex] = element.Weights[vertexIndex];
                        totalWeight += element.Weights[vertexIndex];
                    }
                }
                if (totalWeight > 0.0f)
                {
                    for (int j = 0; j < UBlentSpace_Triangle.MaxVertices; ++j)
                    {
                        newElement.Weights[j] /= totalWeight;
                    }
                }

                GridTriangles.Add(newElement);
            }
        }
        int outSamplesSearchIndex = 0;
        bool OutSamplesFindIndex(BlendSample sample)
        {
            return sample.AnimationIndex == outSamplesSearchIndex;
        }
        int OutSamplesSort(BlendSample a, BlendSample b)
        {
            if (b.TotalWeight < a.TotalWeight)
                return 1;
            if (b.TotalWeight == a.TotalWeight)
                return 0;
            else
                return -1;
        }
        List<WeightedTriangle> mGridWeightedTriangles = new List<WeightedTriangle>();
        public void EvaluateRuntimeSamplesByInput(Vector3 input, ref List<BlendSample> outSamples)
        {
            if (AnimPoints.Count == 0)
                return;
            mGridWeightedTriangles.Clear();
            ExtractWeightedTrianglesByInput(input, mGridWeightedTriangles);
            for (int i = 0; i < mGridWeightedTriangles.Count; ++i)
            {
                var weightedTriangle = mGridWeightedTriangles[i];
                var weight = weightedTriangle.BlendWeight;
                var triangle = weightedTriangle.Triangle;
                for (int index = 0; index < UBlentSpace_Triangle.MaxVertices; ++index)
                {
                    var animationIndex = triangle.Indices[index];
                    outSamplesSearchIndex = animationIndex;
                    if (animationIndex != -1)
                    {
                        var result = outSamples.FindIndex(OutSamplesFindIndex);
                        BlendSample animationData;
                        if (result == -1)
                        {
                            animationData = new BlendSample();
                            animationData.TotalWeight += triangle.Weights[index] * weight;
                            animationData.Animation = AnimPoints[animationIndex].Animation;
                            animationData.AnimationIndex = animationIndex;
                            outSamples.Add(animationData);
                        }
                        else
                        {
                            animationData = outSamples[result];
                            animationData.TotalWeight += triangle.Weights[index] * weight;
                            animationData.AnimationIndex = animationIndex;
                            animationData.Animation = AnimPoints[animationIndex].Animation;
                            outSamples[result] = animationData;
                        }

                    }
                }
            }

            outSamples.Sort(OutSamplesSort);
            float totalWeight = 0;
            for (int i = 0; i < outSamples.Count; ++i)
            {
                totalWeight += outSamples[i].TotalWeight;
                if (outSamples[i].TotalWeight < 0.000001f)
                {
                    outSamples.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < outSamples.Count; ++i)
            {
                var sample = outSamples[i];
                sample.TotalWeight /= totalWeight;
                outSamples[i] = sample;
            }
        }
        #region IAnimationAsset
        public abstract RName AssetName { get; set; }
        public abstract IAssetMeta CreateAMeta();
        public abstract IAssetMeta GetAMeta();
        public abstract void UpdateAMetaReferences(IAssetMeta ameta);
        public abstract void SaveAssetTo(RName name);
        #endregion
    }
}
