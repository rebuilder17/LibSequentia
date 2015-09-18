using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LibSequentia.Data
{
	/// <summary>
	/// Section의 시퀀스. 곡 하나에 해당
	/// </summary>
	public partial class Track
	{
		// Members

		List<Section>		m_sectionSeq	= new List<Section>();			// section의 시퀀스
		IAudioClipPack		m_clipPack;										// 이 track에서 사용하는 오디오 클립 묶음


		/// <summary>
		/// BPM.
		/// </summary>
		public float BPM { get; set; }

		/// <summary>
		/// section 갯수
		/// </summary>
		public int sectionCount
		{
			get
			{
				return m_sectionSeq.Count;
			}
		}



		/// <summary>
		/// AudioClipPack 붙이기
		/// </summary>
		/// <param name="pack"></param>
		public void SetClipPack(IAudioClipPack pack)
		{
			m_clipPack = pack;
		}

		public Section GetSection(int index)
		{
			return m_sectionSeq[index];
		}
	}
}