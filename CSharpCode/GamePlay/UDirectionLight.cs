﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.GamePlay
{
    public class UDirectionLight : IO.BaseSerializer
    {
        public Vector3 mDirection;
        public float mSunLightLeak = 0.05f;
        public Vector3 mSunLightColor;
        public float mSunLightIntensity = 2.5f;
        public Vector3 mSkyLightColor;
        public Vector3 mGroundLightColor;
        [Rtti.Meta]
        public Vector3 Direction
        {
            get => mDirection;
            set => mDirection = value;
        }

        [EGui.Controls.PropertyGrid.Color3PickerEditor]
        [Rtti.Meta]
        public Vector3 SunLightColor
        {
            get => mSunLightColor;
            set
            {
                mSunLightColor = value;
            }
        }
        [EGui.Controls.PropertyGrid.Color3PickerEditor]
        [Rtti.Meta]
        public Vector3 SkyLightColor
        {
            get => mSkyLightColor;
            set
            {
                mSkyLightColor = value;
            }
        }
        [EGui.Controls.PropertyGrid.Color3PickerEditor]
        [Rtti.Meta]
        public Vector3 GroundLightColor
        {
            get => mGroundLightColor;
            set
            {
                mGroundLightColor = value;
            }
        }
        public UDirectionLight()
        {
            mDirection = new Vector3(1, -1, 1);
            mDirection.Normalize();
            mSunLightLeak = 0.05f;

            mSunLightColor = new Vector3(1, 1, 1);
            mSunLightIntensity = 2.5f;
                    
            mSkyLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            mGroundLightColor = new Vector3(0.1f, 0.1f, 0.1f);
        }
    }
}
