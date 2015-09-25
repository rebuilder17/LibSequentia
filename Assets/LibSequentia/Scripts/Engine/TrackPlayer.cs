using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using LibSequentia.Data;

namespace LibSequentia.Engine
{
	/// <summary>
	/// 트랙 재생을 관리
	/// </summary>
	public class TrackPlayer
	{
		/// <summary>
		/// 트랜지션시 시간 정보
		/// </summary>
		public struct TransitionTimeInfo
		{
			public double	transitionStart;		// 트랜지션 시작 - 새 섹션이 시작되는 시점
			public double	transitionEnd;			// 트랜지션 종료 - 새 섹션의 루프 시작 지점
		}


		// Constants

		const int					c_playerCount	= 2;


		// Members

		MonoBehaviour				m_context;
		BeatSyncClock				m_clock;

		SectionPlayer []			m_secplayer		= new SectionPlayer[c_playerCount];
		IAutomationControl []		m_secplayerCtrl	= new IAutomationControl[c_playerCount];
		int							m_secPlayerIdx	= 0;

		Track						m_track;
		int							m_sectionIdx	= 0;
		bool						m_suppressProgress	= false;	// 더 이상의 섹션 진행을 막는다.

		void SwitchPlayer()
		{
			m_secPlayerIdx		= (m_secPlayerIdx + 1) % 2;
		}

		SectionPlayer currentPlayer
		{
			get { return m_secplayer[m_secPlayerIdx]; }
		}

		SectionPlayer sidePlayer
		{
			get { return m_secplayer[(m_secPlayerIdx+1)%2]; }
		}

		float						m_tension;
		/// <summary>
		/// 긴장도 (텐션)
		/// </summary>
		public float tension
		{
			get { return m_tension; }
			set
			{
				m_tension	= value;
				for(int i = 0; i < c_playerCount; i++)
				{
					m_secplayer[i].tension	= m_tension;
				}
			}
		}

		/// <summary>
		/// 현재 재생중인지
		/// </summary>
		public bool isPlaying
		{
			// 둘 중 하나라도 재생중이라면 true
			get { return !m_secplayer[0].isReadyOrFinished || !m_secplayer[1].isReadyOrFinished; }
		}

		/// <summary>
		/// 현재 재생중인 섹션 인덱스
		/// </summary>
		public int currentSectionIndex
		{
			get { return m_sectionIdx; }
		}

		/// <summary>
		/// 총 섹션 갯수
		/// </summary>
		public int sectionCount
		{
			get { return m_track.sectionCount; }
		}

		/// <summary>
		/// 현재 사용중인 비트 싱크 클락을 가져온다.
		/// </summary>
		public BeatSyncClock clock
		{
			get { return m_clock; }
		}



		public TrackPlayer(MonoBehaviour context)
		{
			m_context	= context;
		}

		/// <summary>
		/// SectionPlayer 추가
		/// </summary>
		/// <param name="player"></param>
		/// <param name="layermixers"></param>
		/// <param name="sectionMixerCtrl"></param>
		public void AttachSectionPlayer(SectionPlayer player, IAutomationControl sectionMixerCtrl)
		{
			// 적당한 idx 찾기
			int idx	= 0;
			for(; idx < c_playerCount; idx++)
			{
				if (m_secplayer[idx] == null)
					break;
			}

			if (idx >= c_playerCount)
				throw new System.InvalidOperationException();
			
			//
			m_secplayer[idx]		= player;
			m_secplayerCtrl[idx]	= sectionMixerCtrl;
		}

		/// <summary>
		/// Track을 새로 지정
		/// </summary>
		/// <param name="track"></param>
		/// <param name="clockToSync">기존에 존재하는 clock과 싱크를 맞춰야할 경우 지정</param>
		public void SetTrack(Track track, BeatSyncClock clockToSync = null)
		{
			m_track				= track;
			m_sectionIdx		= -1;
			m_suppressProgress	= false;

			if (clockToSync == null)								// 클럭이 지정되지 않은 경우, 새로 생성
			{
				m_clock			= new BeatSyncClock(track.BPM);
			}
			else if (clockToSync.BPM != track.BPM)					// 기존 클럭과 bpm이 다른 경우, 기존 클럭의 다음번 비트와 동기화하여 새로 클럭 생성
			{
				m_clock			= new BeatSyncClock(track.BPM, clockToSync.CalcNextSafeBeatTime());
			}
			else
			{														// 아니면 기존 클럭 그대로 사용
				m_clock			= clockToSync;
			}
		}

		/// <summary>
		/// 자연 진행
		/// </summary>
		/// <param name="suppressProgress">현재 섹션을 마지막으로 재생을 멈춘다.</param>
		/// <returns>transition이 발생할 시간. transition을 할 수 없었던 경우 -1 리턴</returns>
		public TransitionTimeInfo DoNaturalProgress(bool suppressProgress = false)
		{
			m_suppressProgress	= suppressProgress;
			return DoProgress(SectionPlayer.TransitionType.Natural);
		}

		/// <summary>
		/// 강제 진행
		/// </summary>
		/// <param name="suppressProgress">현재 섹션을 마지막으로 재생을 멈춘다.</param>
		/// <returns>transition이 발생할 시간. transition을 할 수 없었던 경우 -1 리턴</returns>
		public TransitionTimeInfo DoManualProgress(bool suppressProgress = false)
		{
			m_suppressProgress	= suppressProgress;
			return DoProgress(SectionPlayer.TransitionType.Manual);
		}

		/// <summary>
		/// 즉시 진행. 최초 플레이시에만 가능하다
		/// </summary>
		/// <returns></returns>
		public TransitionTimeInfo DoInstantProgress()
		{
			return DoProgress(SectionPlayer.TransitionType.Instant);
		}

		TransitionTimeInfo DoProgress(SectionPlayer.TransitionType ttype)
		{
			// 트랜지션을 해도 괜찮은 상황인지 체크

			if (sidePlayer.isReadyOrFinished)	// 다른쪽 플레이어가 재생중이 아니거나 루프 종료된 경우 스위칭. 새로 재생할 플레이어가 currentPlayer가 된다
			{
				SwitchPlayer();
				m_sectionIdx++;
				Debug.Log("switch player. section idx : " + m_sectionIdx);
			}


			// 트랜지션 시간 구하기

			double transitionTime	= CalcSectionTransitionTimeLength(m_sectionIdx, ttype);



			// 여기서부터는 sidePlayer => 기존에 재생중이던 플레이어가 됨

			TransitionTimeInfo tinfo	= new TransitionTimeInfo();
			tinfo.transitionStart		= -1;
			tinfo.transitionEnd			= -1;

			// 기존 재생중이던 플레이어가 트랜지션 진행중이지 않고, 좀더 상위의 트랜지션을 걸 때
			if (!sidePlayer.isOnTransition
				&& (int)ttype > (int)sidePlayer.currentEndTransition)
			{
				if(m_sectionIdx > 0 && !m_suppressProgress)	// 처음 섹션이 아니고 재생을 끝내는 경우도 아닐 때만
				{
					tinfo.transitionStart	= sidePlayer.FadeoutSection(ttype, transitionTime);
				}
				else
				{
					tinfo.transitionStart	= m_clock.CalcNextSafeBeatTime();
				}


				if (m_sectionIdx < m_track.sectionCount)
				{
					currentPlayer.StartSection(m_track.GetSection(m_sectionIdx), m_clock, ttype, tinfo.transitionStart);
					tinfo.transitionEnd		= currentPlayer.firstLoopStartDspTime;
				}
				else
				{
					// TODO : 필요할지는 모르겠는데, 새 섹션이 시작되지 않는 경우에도 올바른 트랜지션 종료 타이밍을 계산해서 집어넣게 수정해야 할지도...
					tinfo.transitionEnd		= tinfo.transitionStart;
				}
			}
			else
			{
				Debug.Log(string.Format("sidePlayer.isOnTransition : {0}, sidePlayer.currentEndTransition : {1}", sidePlayer.isOnTransition, sidePlayer.currentEndTransition));
			}

			return tinfo;
		}

		/// <summary>
		/// 특정 세션이 전환되어 들어올 때 걸리는 시간을 계산
		/// </summary>
		/// <param name="sectionindex"></param>
		/// <param name="ttype"></param>
		/// <returns></returns>
		public double CalcSectionTransitionTimeLength(int sectionindex, SectionPlayer.TransitionType ttype)
		{
			int trbeats				= 4;
			if (sectionindex < m_track.sectionCount)				// 다음에 올 섹션이 있다면 해당 섹션의 앞쪽 부분을 기준으로 계산
			{
				var nextsec			= m_track.GetSection(sectionindex);
				trbeats				= nextsec.beatStart;

				if (ttype == SectionPlayer.TransitionType.Manual)	// 강제 전환은 fillin 앞부분이 잘림
				{
					trbeats			-= nextsec.beatFillIn;
				}
			}

			return m_clock.CalcBeatTimeLength(trbeats);
		}

		/// <summary>
		/// 재생 즉시 모두 정지
		/// </summary>
		public void StopImmediately()
		{
			currentPlayer.EndSection();
			sidePlayer.EndSection();
		}
	}
}