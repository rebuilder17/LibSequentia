using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace LibSequentia.Engine
{
	/// <summary>
	/// TrackPlayer 두 개를 운용하고, 둘 사이의 전환을 실행하는 플레이어
	/// </summary>
	public class MasterPlayer
	{
		// Constants

		const int				c_playerCount	= 2;


		/// <summary>
		/// MasterPlayer 내부에서 사용하는 메세지
		/// </summary>
		struct Message
		{
			public enum Type
			{
				NaturalProgress,		// 자연진행
				ManualProgress,			// 강제진행

				NewTrack,				// 새 트랙 올리기
				NewTransitionScenario,	// 새 트랜지션 시나리오
				BothTrack,				// 양쪽 트랙 모두 세팅

				StepTo,					// 특정 스텝으로 이동
			}

			public Type		type;
			public object	parameter;
			public object	parameter2;
		}

		/// <summary>
		/// 플레이어 상태
		/// </summary>
		enum State
		{
			NotPlaying,					// 아무 트랙도 재생중이지 않음
			PlayingOneSide,				// 한쪽에서만 재생중
			TransitionReady,			// 다른 트랙 재생 준비됨
			OnTransition,				// 트랜지션 진행중
			TransitionFinish,			// 트랜지션 완료
		}



		// Members

		TrackPlayer []				m_trackPlayer		= new TrackPlayer[c_playerCount];				// track 플레이어
		Data.IAutomationControl []	m_trackTransCtrls	= new Data.IAutomationControl[c_playerCount];	// track 전환시 사용하는 오토메이션 컨트롤러

		int							m_playerIdx;														// 현재의 track 플레이어 인덱스

		TrackPlayer currentPlayer
		{
			get { return m_trackPlayer[m_playerIdx]; }
		}

		TrackPlayer sidePlayer
		{
			get { return m_trackPlayer[(m_playerIdx+1)%2]; }
		}

		void SwitchPlayer()
		{
			m_playerIdx	= (m_playerIdx + 1) % 2;
		}

		MonoBehaviour			m_context;
		Queue<Message>			m_msgQueue	= new Queue<Message>();		// 플레이어 내부 메세지 큐

		/// <summary>
		/// 가장 최근에 계산된 전환 시간.
		/// </summary>
		public double lastCalculatedTransitionTime { get; private set; }

		bool					m_newTrackReady;						// 새로 재생할 (트랜지션할) 트랙이 준비되어있는지 여부
		bool					m_bothTrackReady;						// 리셋 후 최초 재생 시작시. 양쪽 트랙이 다 준비되었는지 여부
		bool					m_transitionReserved;					// 전환 효과 예약되었는지 여부
		SectionPlayer.TransitionType m_transitionType;					// 전환 타입
		State					m_state;								// 현재 플레이어 상태
		bool					m_reverse;								// 역진행 여부

		Data.TransitionScenario	m_tranScenario;							// 전환 시나리오


		/// <summary>
		/// 음악의 긴장도
		/// </summary>
		public float tension
		{
			set
			{
				m_trackPlayer[0].tension	= value;
				m_trackPlayer[1].tension	= value;
			}
		}


		float	m_transition;

		/// <summary>
		/// 곡 전환시 트랜지션
		/// </summary>
		public float transition
		{
			get { return m_transition; }
			set
			{
				m_transition	= value;
				if (m_tranScenario != null)
				{
					m_tranScenario.transition	= value;
				}
			}
		}






		public MasterPlayer(MonoBehaviour context)
		{
			m_state		= State.NotPlaying;
			m_context	= context;
			context.StartCoroutine(UpdateCo());
		}

		public void SetTrackPlayers(TrackPlayer p1, TrackPlayer p2)
		{
			m_trackPlayer[0]	= p1;
			m_trackPlayer[1]	= p2;
		}

		public void SetTransitionCtrls(Data.IAutomationControl p1, Data.IAutomationControl p2)
		{
			m_trackTransCtrls[0]	= p1;
			m_trackTransCtrls[1]	= p2;
		}

		public void DoNaturalProgress()
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NaturalProgress });
		}

		public void DoManualProgress()
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.ManualProgress });
		}

		public void ProgressStepTo(int step, bool reverse)
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.StepTo, parameter = step, parameter2 = reverse });
		}

		/// <summary>
		/// 새 트랙 올리기. 재생은 하지 않음. 다음번 진행 타이밍에 맞춰서 시작
		/// </summary>
		/// <param name="track"></param>
		public void SetNewTrack(Data.Track track, Data.TransitionScenario trans)
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NewTrack, parameter = track });
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NewTransitionScenario, parameter = trans });
		}

		/// <summary>
		/// 두 트랙 모두 설정 (초기화 목적)
		/// </summary>
		/// <param name="mainTrack"></param>
		/// <param name="sideTrack"></param>
		/// <param name="trans"></param>
		public void SetBothTracks(Data.Track mainTrack, Data.Track sideTrack, Data.TransitionScenario trans)
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.BothTrack, parameter = mainTrack, parameter2 = sideTrack });
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NewTransitionScenario, parameter = trans });
		}

		private bool ProcessMessage(ref Message msg)
		{
			bool consume;
			switch(msg.type)
			{
				case Message.Type.ManualProgress:
					{
						var trtime	= currentPlayer.DoManualProgress();
						consume		= (trtime.transitionStart >= 0);

						if (consume)	// 정상적으로 처리된 경우에만 trtime을 트랜지션 시간으로 인정하여 보관한다.
						{
							lastCalculatedTransitionTime = trtime.transitionEnd;
							m_transitionType		= SectionPlayer.TransitionType.Manual;
							m_transitionReserved	= true;
						}
					}
					break;

				case Message.Type.NaturalProgress:
					{
						var trtime	= currentPlayer.DoNaturalProgress();
						consume		= (trtime.transitionStart >= 0);

						if (consume)	// 정상적으로 처리된 경우에만 trtime을 트랜지션 시간으로 인정하여 보관한다.
						{
							lastCalculatedTransitionTime = trtime.transitionEnd;
							m_transitionType		= SectionPlayer.TransitionType.Natural;
							m_transitionReserved	= true;
						}
					}
					break;

				case Message.Type.NewTrack:

					if (!sidePlayer.isPlaying && !m_newTrackReady)	// 반대쪽 플레이어가 현재 재생중이 아닌 경우만 세팅한다.
					{
						sidePlayer.SetTrack(msg.parameter as Data.Track, currentPlayer.clock);
						m_newTrackReady	= true;
						consume			= true;
					}
					else
					{
						consume = false;
					}
					break;

				case Message.Type.NewTransitionScenario:
					m_tranScenario	= msg.parameter as Data.TransitionScenario;
					consume	= true;
					break;

				case Message.Type.BothTrack:
					if (m_state != State.NotPlaying)
					{
						Debug.LogError("BothTrack message should be used only for initialization purpose.");
					}

					currentPlayer.SetTrack(msg.parameter as Data.Track);
					sidePlayer.SetTrack(msg.parameter2 as Data.Track, currentPlayer.clock);

					m_bothTrackReady	= true;
					m_newTrackReady		= true;
					consume	= true;
					break;

				case Message.Type.StepTo:
					{
						var step	= (int)msg.parameter;
						var reverse	= (bool)msg.parameter2;
						var trtime	= currentPlayer.StepTo(step, reverse);
						consume		= (trtime.transitionStart >= 0);

						if (consume)
						{
							lastCalculatedTransitionTime = trtime.transitionEnd;
							m_transitionType		= SectionPlayer.TransitionType.Natural;	// NOTE : 현재 이거 무의미해서 그냥 안고치고 이대로 둠....
							m_transitionReserved	= true;
							m_reverse				= reverse;
						}
					}
					break;

				default:
					Debug.LogError("unknown message : " + msg.type);
					consume	= true;
					break;
			}

			return consume;
		}

		/// <summary>
		/// 스테이트 - 아무것도 재생중이지 않을 때
		/// </summary>
		private void ProcessState_NotPlaying()
		{
			if (m_newTrackReady && !m_bothTrackReady)	// 새 트랙이 올라왔다면 바로 처리해준다 (단, 두 트랙 모두 초기화한 경우에는 제외)
			{
				m_newTrackReady	= false;

				SwitchPlayer();
			}
			
			if (m_transitionReserved)				// 트랜지션 시작 => 첫 재생임
			{
				m_state			= State.PlayingOneSide;

				// 두 트랙을 모두 초기화한 경우의 플래그는 여기서는 꺼준다.
				// 위에서 처리가 되지 않았으므로 m_newTrackReady는 여전히 true일 것이고,
				// 따라서 트랜지션 후 반대쪽 트랙도 재생/트랜지션 시나리오 적용 작업이 이루어질 것임.
				m_bothTrackReady	= false;
			}
		}

		/// <summary>
		/// 스태이트 - 한 쪽에서만 재생중
		/// </summary>
		private void ProcessState_PlayngOneSide()
		{
			if (m_transitionReserved)					// 트랜지션 예약되었을 시
			{
				if (m_newTrackReady)					// 새 트랙이 대기중이라면
				{
					var newclock	= sidePlayer.clock;
					var nextbeat	= newclock.CalcNextSafeBeatTime();
					if (lastCalculatedTransitionTime - nextbeat < newclock.SecondPerBeat)	// 다음 비트에 트랜지션이 온다면 새 트랙 재생 시작
					{
						SwitchPlayer();
						currentPlayer.DoInstantProgress(m_reverse);							// fill-in 부분을 생략하고 즉시 재생을 한다.

						m_state					= State.TransitionReady;					// 스테이트 변화
						m_newTrackReady			= false;

						if (m_tranScenario != null)											// TransitionScenario 준비
						{
							m_tranScenario.SetAutomationTargets(m_trackTransCtrls[m_playerIdx], m_trackTransCtrls[(m_playerIdx + 1) % 2]);
							m_tranScenario.reverseTransition	= m_reverse;
							//transition			= m_tranScenario.reverseTransition? 1 : 0;
						}
					}
				}
				else
				{
					if (AudioSettings.dspTime >= lastCalculatedTransitionTime)				// 별 일 없이 트랜지션 시간을 지나는 경우에 플래그를 꺼준다.
					{
						m_transitionReserved	= false;
						m_state					= State.PlayingOneSide;
					}
				}
			}
			else if (!currentPlayer.isPlaying && !sidePlayer.isPlaying)						// 트랜지션중이 아닌데 플레이중도 아니라면, notplaying 스테이트로
			{
				Debug.Log(string.Format("current player playing : {0}, side player playing : {1}", currentPlayer.isPlaying, sidePlayer.isPlaying));
				m_state	= State.NotPlaying;
			}
		}
		/// <summary>
		/// 스테이트 - 트랜지션 대기
		/// </summary>
		private void ProcessState_TransitionReady()
		{
			if (AudioSettings.dspTime >= lastCalculatedTransitionTime)					// 트랜지션 시간이 되었다면 본격적인 트랜지션 상태로
			{
				m_state		= State.OnTransition;
				m_transitionReserved	= false;
			}
		}

		/// <summary>
		/// 스테이트 - 트랜지션중
		/// </summary>
		private void ProcessState_OnTransition()
		{
			if (m_transitionReserved)					// 트랜지션 예약되었을 시
			{
				var clock		= currentPlayer.clock;
				var nextbeat	= clock.CalcNextSafeBeatTime();
				if (lastCalculatedTransitionTime - nextbeat < clock.SecondPerBeat)		// 다음 비트에 트랜지션한다면
				{
					m_state					= State.TransitionFinish;
				}
			}
		}

		/// <summary>
		/// 스테이트 - 트랜지션 완료
		/// </summary>
		private void ProcessState_TransitionFinish()
		{
			if (AudioSettings.dspTime >= lastCalculatedTransitionTime)					// 트랜지션 시간이 되었다면 트랜지션 완전 마무리
			{
				m_state	= State.PlayingOneSide;
				m_transitionReserved	= false;

				if (m_tranScenario != null)												// TransitionScenario 끝내기
				{
					transition		= m_tranScenario.reverseTransition? 0 : 1;
					m_tranScenario	= null;
				}

				sidePlayer.StopImmediately();											// 한쪽 플레이어 급히 정지
			}
		}

		State __oldstate;
		IEnumerator UpdateCo()
		{
			while (true)
			{
				// 메세지 큐 처리
				if (m_msgQueue.Count > 0)
				{
					var msg	= m_msgQueue.Peek();

					if (ProcessMessage(ref msg))		// 메세지를 정상적으로 처리하면 큐에서 삭제
					{
						Debug.Log(msg.type.ToString() + " message consumed");
						m_msgQueue.Dequeue();
					}
					else
					{
						Debug.Log(msg.type.ToString() + " message not consumed...");
						yield return null;
					}
				}
				else
				{
					yield return null;
				}

				if (__oldstate != m_state)
				{
					Debug.Log("state changed : " + m_state);
					__oldstate = m_state;
				}

				// 현재 스테이트에 맞는 처리
				switch(m_state)
				{
					case State.NotPlaying:
						ProcessState_NotPlaying();
						break;

					case State.PlayingOneSide:
						ProcessState_PlayngOneSide();
						break;

					case State.TransitionReady:
						ProcessState_TransitionReady();
						break;

					case State.OnTransition:
						ProcessState_OnTransition();
						break;

					case State.TransitionFinish:
						ProcessState_TransitionFinish();
						break;
				}
			}
		}
	}
}