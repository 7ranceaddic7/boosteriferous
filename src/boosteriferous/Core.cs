﻿using System;
using System.Collections.Generic;
using UnityEngine;
using boosteriferous.Modules;

namespace boosteriferous
{
	public abstract class ProfileShape
	{
		public abstract string name { get; }
		public virtual void setFieldVisibility(ModuleControlledFirework mcf)
		{
			setFieldInvisible(mcf, "throttleDownPoint");
			setFieldInvisible(mcf, "throttleDownAmount");
			setFieldInvisible(mcf, "throttleUpAmount");
		}
		public abstract void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale);
		public bool disable = false;
		public string techRequired = null;
		public float costFactor = 0f;
		public float minThrottleFactor = 1f;
		public bool isAvailable(ModuleControlledFirework mcf)
		{
			if (disable)
				return false;
			if (!string.IsNullOrEmpty(techRequired) && ResearchAndDevelopment.GetTechnologyState(techRequired) != RDTech.State.Available)
				return false;
			return true;
		}
		private void setFieldVisibleState(ModuleControlledFirework mcf, string fieldName, bool state)
		{
			mcf.Fields[fieldName].guiActive = state;
			mcf.Fields[fieldName].guiActiveEditor = state;
		}
		public void setFieldVisible(ModuleControlledFirework mcf, string fieldName)
		{
			setFieldVisibleState(mcf, fieldName, true);
		}
		public void setFieldInvisible(ModuleControlledFirework mcf, string fieldName)
		{
			setFieldVisibleState(mcf, fieldName, false);
		}
	};

	public class ProfileFlat : ProfileShape
	{
		public override string name { get { return "Flat"; } }
		public override void setFieldVisibility(ModuleControlledFirework mcf)
		{
			base.setFieldVisibility(mcf);
			setFieldVisible(mcf, "throttleDownAmount");
		}
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			float tda = mcf.throttleDownAmount / 100f;
			Debug.LogFormat("[bfer] Recalculating thrust curve: flat, tda = {0:F3}", tda);
			timeScale = tda > 0f ? 1f / tda : 0f;
			fc = new FloatCurve();
			// Curve is backwards, because that's how thrustCurve works
			fc.Add(0f, 1f, 0f, 0f);
			fc.Add(1f, 1f, 0f, 0f);
		}
	}

	public class ProfileStep : ProfileShape
	{
		public override string name { get { return "Step"; } }
		public override void setFieldVisibility(ModuleControlledFirework mcf)
		{
			base.setFieldVisibility(mcf);
			setFieldVisible(mcf, "throttleDownPoint");
			setFieldVisible(mcf, "throttleDownAmount");
		}
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			float tdp = mcf.throttleDownPoint / 100f, tda = mcf.throttleDownAmount / 100f;
			Debug.LogFormat("[bfer] Recalculating thrust curve: step, tdp = {0:F3}, tda = {1:F3}", tdp, tda);
			// Have to multiply curve points by this to scale maxThrust (almost) correctly
			timeScale = (1f - tdp) + (tda > 0f ? tdp / tda : 0f);
			fc = new FloatCurve();
			// Curve is backwards, because that's how thrustCurve works
			fc.Add(0f, tda * timeScale, 0f, 0f);
			fc.Add(tdp - mcf.rampWidth, tda * timeScale, 0f, 0f);
			fc.Add(tdp + mcf.rampWidth, timeScale, 0f, 0f);
			fc.Add(1f, timeScale, 0f, 0f);
		}
	}

	// Default profile for 'realistic' SRBs: thrust increases over duration of burn
	public class ProfileProgressive : ProfileShape
	{
		public override string name { get { return "Progressive"; } }
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			// T = 1 - 0.2P
			// t = integ[1/(1-0.2P) dP] = (1/0.2) ln (1/(1-0.2P))
			// so burn time = 5 ln (1.25)
			Debug.LogFormat("[bfer] Recalculating thrust curve: progressive");
			// Have to multiply curve points by this to scale maxThrust (almost) correctly
			timeScale = (float)(5f * Math.Log(1.25));
			fc = new FloatCurve();
			fc.Add(0f, timeScale, 0f, -0.2f);
			fc.Add(1f, timeScale * 0.8f, -0.2f, 0f);
		}
	}

	public class ProfileLinear : ProfileShape
	{
		public override string name { get { return "Linear"; } }
		public override void setFieldVisibility(ModuleControlledFirework mcf)
		{
			base.setFieldVisibility(mcf);
			setFieldVisible(mcf, "throttleDownAmount");
		}
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			float tda = mcf.throttleDownAmount / 100f;
			// T = tda + P(1-tda); let a = tda, b = 1-tda; then T = a + bP
			// t = integ[1/(a+bP) dP] = 1/b ln((a+b) / a) = (1/(1-tda)) ln(1/tda) = ln(tda) / (tda-1)
			// special cases:
			//  tda = 0 => b = 1 => t = -ln(0) is infinite
			//  tda = 1 => b = 0 => t = integ[dP] = 1
			Debug.LogFormat("[bfer] Recalculating thrust curve: linear, tda = {0:F3}", tda);
			if (tda <= 0f)
			{
				Debug.LogErrorFormat("[bfer] bad tda, falling back to 1.0");
				tda = 1f;
			}
			if (tda >= 1f)
			{
				// degenerate to Flat
				timeScale = 1f;
				fc = new FloatCurve();
				fc.Add(0f, 1f, 0f, 0f);
				fc.Add(1f, 1f, 0f, 0f);
			}
			else
			{
				timeScale = (float)Math.Log(tda) / (tda - 1f);
				fc = new FloatCurve();
				fc.Add(0f, timeScale * tda, 0f, (1f - tda));
				fc.Add(1f, timeScale, (1f - tda), 0f);
			}
		}
	}

	public class ProfileStepLinear : ProfileShape
	{
		public override string name { get { return "StepLinear"; } }
		public override void setFieldVisibility(ModuleControlledFirework mcf)
		{
			base.setFieldVisibility(mcf);
			setFieldVisible(mcf, "throttleDownPoint");
			setFieldVisible(mcf, "throttleDownAmount");
		}
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			float tda = mcf.throttleDownAmount / 100f, tdp = mcf.throttleDownPoint / 100f;
			// T = { 1 - (1-a)P/p [P < p] or 1 [P > p] }, where a = tda, p = tdp
			// t = (p ln a)/(a - 1) + (1 - p)
			// special cases:
			//  tda = 0 => t is infinite
			//  tda = 1 => t = 1
			Debug.LogFormat("[bfer] Recalculating thrust curve: step-linear, tda = {0:F3}, tdp = {1:F3}", tda, tdp);
			if (tda <= 0f)
			{
				Debug.LogErrorFormat("[bfer] bad tda, falling back to 1.0");
				tda = 1f;
			}
			if (tdp <= mcf.rampWidth)
			{
				Debug.LogErrorFormat("[bfer] bad tdp, falling back to {0:F3}", mcf.rampWidth);
				tdp = mcf.rampWidth;
			}
			if (tda >= 1f)
			{
				// degenerate to Flat
				timeScale = 1f;
				fc = new FloatCurve();
				fc.Add(0f, 1f, 0f, 0f);
				fc.Add(1f, 1f, 0f, 0f);
			}
			else
			{
				timeScale = tdp * (float)Math.Log(tda) / (tda - 1f) + 1f - tdp;
				fc = new FloatCurve();
				fc.Add(0f, timeScale, 0f, (tda - 1f) / tdp);
				fc.Add(tdp - mcf.rampWidth, timeScale * tda, (tda - 1f) / tdp, 0f);
				fc.Add(tdp + mcf.rampWidth, timeScale, 0f, 0f);
				fc.Add(1f, timeScale, 0f, 0f);
			}
		}
	}

	public class ProfileStepLinear2 : ProfileShape
	{
		public override string name { get { return "StepLinear2"; } }
		public override void setFieldVisibility(ModuleControlledFirework mcf)
		{
			base.setFieldVisibility(mcf);
			setFieldVisible(mcf, "throttleDownPoint");
			setFieldVisible(mcf, "throttleDownAmount");
			setFieldVisible(mcf, "throttleUpAmount");
		}
		public override void recalcCurve(ModuleControlledFirework mcf, out FloatCurve fc, out float timeScale)
		{
			float tda = mcf.throttleDownAmount / 100f, tdp = mcf.throttleDownPoint / 100f, tua = mcf.throttleUpAmount / 100f;
			// T = { u - (u-a)P/p [P < p] or 1 [P > p] }, where a = tda, p = tdp, u = tua
			// t = (p ln (a/u))/(a - u) + (1 - p)
			// special cases:
			//  tda = 0 => t is infinite
			//  tda = 1 => t = 1
			//  tua = 0 => t is infinite
			Debug.LogFormat("[bfer] Recalculating thrust curve: step-linear, tda = {0:F3}, tdp = {1:F3}, tua = {2:F3}", tda, tdp, tua);
			if (tda <= 0f)
			{
				Debug.LogErrorFormat("[bfer] bad tda, falling back to 1.0");
				tda = 1f;
			}
			if (tua <= 0f)
			{
				Debug.LogErrorFormat("[bfer] bad tua, falling back to 1.0");
				tua = 1f;
			}
			if (tdp <= mcf.rampWidth)
			{
				Debug.LogErrorFormat("[bfer] bad tdp, falling back to {0:F3}", mcf.rampWidth);
				tdp = mcf.rampWidth;
			}
			if (tda >= 1f)
			{
				// degenerate to Flat
				timeScale = 1f;
				fc = new FloatCurve();
				fc.Add(0f, 1f, 0f, 0f);
				fc.Add(1f, 1f, 0f, 0f);
			}
			else
			{
				timeScale = tdp * (float)Math.Log(tda / tua) / (tda - tua) + 1f - tdp;
				fc = new FloatCurve();
				fc.Add(0f, timeScale * tua, 0f, (tda - tua) / tdp);
				fc.Add(tdp - mcf.rampWidth, timeScale * tda, (tda - tua) / tdp, 0f);
				fc.Add(tdp + mcf.rampWidth, timeScale, 0f, 0f);
				fc.Add(1f, timeScale, 0f, 0f);
			}
		}
	}

	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class Core : MonoBehaviour
	{
		public static Core Instance { get; protected set; }
		public Core()
		{
			if (Core.Instance != null)
			{
				Destroy(this);
				return;
			}

			Core.Instance = this;
			profiles = new Dictionary<string, ProfileShape>();
			addProfile(new ProfileFlat());
			addProfile(new ProfileStep());
			addProfile(new ProfileProgressive());
			addProfile(new ProfileLinear());
			addProfile(new ProfileStepLinear());
			addProfile(new ProfileStepLinear2());
		}

		public string defaultProfile = "Flat";

		public Dictionary<string, ProfileShape> profiles;
		private void addProfile(ProfileShape ps)
		{
			profiles.Add(ps.name, ps);
		}

		protected void Start()
		{
			foreach (ConfigNode cn in GameDatabase.Instance.GetConfigNodes("BoosteriferousProfileShape"))
			{
				if (!cn.HasValue("name"))
				{
					Debug.Log("[bfer] No name in a BoosteriferousProfileShape node");
					continue;
				}
				string name = cn.GetValue("name");
				if (!profiles.ContainsKey(name))
				{
					Debug.LogErrorFormat("[bfer] No such BoosteriferousProfileShape '{0}'", name);
					continue;
				}
				ProfileShape ps = profiles[name];
				if (cn.HasValue("techRequired"))
				{
					ps.techRequired = cn.GetValue("techRequired");
				}
				bool isDefault = false;
				if (cn.TryGetValue("isDefault", ref isDefault) && isDefault)
					defaultProfile = name;
				if (cn.HasValue("costFactor"))
				{
					if (!float.TryParse(cn.GetValue("costFactor"), out ps.costFactor))
						Debug.LogWarningFormat("[bfer] Malformed costFactor '{0}' in BoosteriferousProfileShape '{1}'", cn.GetValue("costFactor"), name);
				}
				if (cn.HasValue("minThrottleFactor"))
				{
					if (!float.TryParse(cn.GetValue("minThrottleFactor"), out ps.minThrottleFactor))
						Debug.LogWarningFormat("[bfer] Malformed minThrottleFactor '{0}' in BoosteriferousProfileShape '{1}'", cn.GetValue("minThrottleFactor"), name);
				}
				cn.TryGetValue("disable", ref ps.disable);
			}
		}
	}
}