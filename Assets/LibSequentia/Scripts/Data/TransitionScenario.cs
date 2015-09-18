﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace LibSequentia.Data
{
	/// <summary>
	/// Track 사이의 전환 automation
	/// </summary>
	public class TransitionScenario
	{
		// Members

		List<Automation>	m_introAutomations = new List<Automation>();	// 인트로가 들어가는 Track에 대한 오토메이션
		List<Automation>	m_outroAutomations = new List<Automation>();	// 아웃트로가 들어가는 Track에 대한 오토메이션
		
		IAutomationControl	m_introAutoTarget;								// 오토메이션 대상 (인트로)
		IAutomationControl	m_outroAutoTarget;								// 오토메이션 대상 (아웃트로)

		float				m_transitionRatio;								// 전환 비율

		/// <summary>
		/// 역전환인지 여부. true일 경우 전환 비율을 반대로 적용한다. (0~1 -> 1~0)
		/// 전환 비율을 설정할 시에는 보통 때와 마찬가지로 전환되지 않음을 0, 완전히 전환됨을 1 로 생각하면 된다.
		/// </summary>
		public bool reverseTransition { get; set; }

		/// <summary>
		/// 전환 비율
		/// </summary>
		public float transition
		{
			get { return m_transitionRatio; }
			set
			{
				m_transitionRatio	= value;
				float ratio			= reverseTransition? (1.0f - value) : value;

				if (m_introAutoTarget != null)
					m_introAutoTarget.Set(ratio, m_introAutomations);

				if (m_outroAutoTarget != null)
					m_outroAutoTarget.Set(ratio, m_outroAutomations);
			}
		}
	}
}
