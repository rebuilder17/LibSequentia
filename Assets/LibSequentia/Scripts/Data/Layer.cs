using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LibSequentia.Data
{
	/// <summary>
	/// Section 을 구성하는 음원 트랙 1개 + 부가 정보
	/// </summary>
	public partial class Layer
	{
		// Members

		List<Automation>	m_tensionAutomations = new List<Automation>();	// Tension 에 따른 오토메이션 데이터 (timeRatio 대신 tension값이 들어가게 된다)

		IAutomationControl	m_autoTarget;			// 오토메이션 대상
		float				m_tension;				// tension값


		/// <summary>
		/// tension 값을 설정한다
		/// </summary>
		public float tension
		{
			get { return m_tension; }
			set
			{
				m_tension	= value;
				if (m_autoTarget != null)
					m_autoTarget.Set(value, m_tensionAutomations);
			}
		}

		/// <summary>
		/// 오디오 클립 핸들
		/// </summary>
		public IAudioClipHandle clipHandle { get; private set; }


		/// <summary>
		/// tension 오토메이션 클립 추가
		/// </summary>
		/// <param name="auto"></param>
		public void AddTensionAutomation(Automation auto)
		{
			m_tensionAutomations.Add(auto);
		}

		public void SetAutomationTarget(IAutomationControl control)
		{
			m_autoTarget = control;
		}
	}
}
