using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LibSequentia.Engine
{
	/// <summary>
	/// 플레이어를 step 방식에 맞게, 무결성을 유지하며 조종할 수 있게 하는 클래스
	/// </summary>
	public class StepControl
	{
		abstract class BaseRequest
		{
			bool m_started	= false;

			protected StepControl	ctrl { get; private set; }
			protected MasterPlayer player
			{
				get { return ctrl.m_player; }
			}
			public bool isComplete { get; protected set; }

			public BaseRequest(StepControl ctrl)
			{
				this.ctrl	= ctrl;
			}

			/// <summary>
			/// 요청을 실제로 시작한다. 딱 한 번만 호출됨
			/// </summary>
			public void Start()
			{
				if(!m_started)
				{
					m_started	= OnStart();
				}
			}

			public void Update()
			{
				if (m_started && !isComplete)	// 정상적으로 시작되고, 아직 완료되지 않은 경우에만 Update
					OnUpdate();
			}

			/// <summary>
			/// 시작 성공하면 true 리턴
			/// </summary>
			/// <returns></returns>
			protected virtual bool OnStart() { return true; }
			protected virtual void OnUpdate() { }
		}


		/// <summary>
		/// 스텝 이동 요청
		/// </summary>
		class StepMoveRequest : BaseRequest
		{
			int			m_targetStep;
			bool		m_reverse;

			Data.Track	m_newTrack;
			Data.TransitionScenario m_trans;

			System.Action	m_stepmove_consume_callback;
			System.Action	m_transition_callback;

			bool		m_wasNaturalTransition = false;

			public int targetStep { get { return m_targetStep; } }
			public bool reverse { get { return m_reverse; } }

			public StepMoveRequest(StepControl player) : base(player) { }


			public void Setup(int targetStep, bool reverse, Data.Track newtrack = null, Data.TransitionScenario trans = null)
			{
				m_targetStep	= targetStep;
				m_reverse		= reverse;
				m_newTrack		= newtrack;
				m_trans			= trans;

				m_wasNaturalTransition	= (targetStep % 2 == 1);

				Debug.Log(string.Format("[stepmove setup] targetStep : {0} reverse : {1} ... ctrl.m_newTrackIsOn : {2}", targetStep, reverse, ctrl.m_newTrackIsOn));
			}

			protected override bool OnStart()
			{
				if (m_newTrack != null)			// 새 트랙을 올리는 경우
				{
					var handle_newtrack	= player.SetNewTrack(m_newTrack, m_trans)[0];
					handle_newtrack.consumedDelegate = () =>
						{
							ctrl.m_newTrackIsOn	= true;
							ctrl.m_newTrackStep	= m_reverse? m_newTrack.sectionCount * 2 : 2;
							Debug.Log("m_newTrackIsOn : true, m_newTrackStep : " + ctrl.m_newTrackStep);
						};
				}

				// 현재 재생중인 새 트랙을 캔슬해야하는 경우인지 체크
				//bool newtrackCancel = false;
				if (m_newTrack == null && ctrl.m_newTrackIsOn && ctrl.m_lastMoveWasReversed != m_reverse)
				{
					// 이 요청에 새 트랙 올리기가 없고, 현재 새 트랙이 진행중이며, 진행 방향이 바뀐 경우엔 다시 이전의 상태로 되돌아가야하는 것임 (다음 전환시에 새 트랙이 재생되지 말아야함)
					//newtrackCancel = true;

					var handle_canceltrack	= player.CancelNewTrack();
					handle_canceltrack.consumedDelegate = () =>
						{
							ctrl.m_newTrackIsOn	= false;
						};
				}

				//
				var handle_stepmove	= player.ProgressStepTo(m_targetStep, m_reverse);
				m_stepmove_consume_callback = () =>		// * MoveOnceMore 에서 같은 콜백을 호출할 수 있도록 보관해둔다.
					{
						if (m_newTrack != null || !ctrl.m_newTrackIsOn)
						{
							// 이 요청이 새 트랙 재생을 요구하거나 (그래서 지금 올라온 새 트랙은 현재 재생중인 것이 아님), 기존에 새 트랙이 없을 시엔 curTrackStep을 건드린다.
							// 트랙 트랜지션 상태에서 다시 반대방향으로 빠져나가는 경우엔 앞선 요청에서 (cancelTrack) m_newTrackIsOn 을 false로 두었으므로 별도 조건은 없어도 된다.
							ctrl.m_curTrackStep		= m_targetStep;
							//if (m_targetStep % 2 == 1)	// 자연 진행일 경우, m_curTrackStep은 강제 진행시의 step 인덱스로 세팅해줘야한다.
							//{
							//	ctrl.m_curTrackStep += m_reverse? -1 : 1;
							//}
							Debug.Log("m_curTrackStep <- m_targetStep : " + ctrl.m_curTrackStep);
						}
						else
						{
							// 아닌 경우엔 새로 올라와있는 트랙의 step을 건드려야하므로 newTrackStep
							ctrl.m_newTrackStep		= m_targetStep;
						}
					};
				handle_stepmove.consumedDelegate	= m_stepmove_consume_callback;
				//
				m_transition_callback = () =>	// * MoveOnceMore 에서 같은 콜백을 호출할 수 있도록 보관해둔다.
					{
						if (m_newTrack == null && ctrl.m_newTrackIsOn)	// 이 요청이 새 트랙 재생을 요구하지 않은 경우, 진행중이던 새 트랙이 만약 있었다면
						{
							ctrl.m_newTrackIsOn = false;				// 트랜지션 후 그 트랙으로 넘어갔을 것이기에 플래그를 꺼준다.
							ctrl.m_curTrackStep	= ctrl.m_newTrackStep;	// 스텝도 이제부터는 기존에 새로 올라갔던 트랙의 스텝을 따라가야함
							Debug.Log("m_curTrackStep <- m_newTrackStep : " + ctrl.m_newTrackStep);
						}

						if (m_wasNaturalTransition)						// 자연 트랜지션이었을 경우, 현재 스텝을 더해준다
						{
							ctrl.m_curTrackStep += m_reverse? -1 : 1;
							Debug.Log("(because of m_wasNaturalTransition) ctrl.m_curTrackStep++ : " + ctrl.m_curTrackStep);
						}

						ctrl.m_lastMoveWasReversed	= m_reverse;		// 진행 방향도 저장
						isComplete	= true;								// 트랜지션이 종료되었으므로 요청 완료
					};
				handle_stepmove.transitionDelegate	= m_transition_callback;
				//
				handle_stepmove.notConsumedDelegate = () =>
					{
						Debug.LogWarning("stepmove ignored! m_targetStep : " + m_targetStep);
						handle_stepmove.SetIgnore();
					};

				return true;
			}

			/// <summary>
			/// 한번 더 움직인다. 같은 방향으로 움직여서 자연 진행 => 강제 진행으로 바뀌는 경우에 해당
			/// </summary>
			/// <param name="reverse"></param>
			/// <returns></returns>
			public bool MoveOnceMore(bool reverse)
			{
				// 진행 방향이 다르거나 현재의 타겟 스텝이 홀수(자연진행)이 아닌 경우엔 리턴. (별도의 stepmove 요청으로 처리해야 한다)
				if (m_reverse != reverse || m_targetStep % 2 != 1)
					return false;

				//m_targetStep++;
				var handle	= player.ProgressStepTo(m_targetStep + (m_reverse? -1 : 1), m_reverse);
				// 이쪽 방향 콜백은 원래의 것을 그대로 사용한다.
				handle.consumedDelegate		= m_stepmove_consume_callback;
				handle.transitionDelegate	= m_transition_callback;

				handle.notConsumedDelegate	= () =>
					{
						// 처리되지 못하는 경우가 생긴다면 아마도 이전에 시도한 자연 전환이 진행중이기 때문일 것임.
						// 따로 처리가 필요하지 않은 경우이므로 이 메세지는 무시하게 한다.
						handle.SetIgnore();
					};

				return true;
			}
		}

		/// <summary>
		/// 트랙 하나만 올리고 새로 재생을 시작하는 요청
		/// </summary>
		class NewStartOneTrackRequest : BaseRequest
		{
			// Members

			Data.Track	m_newTrack;
			bool		m_reverse;


			public void Setup(Data.Track track, bool reverse)
			{
				m_newTrack	= track;
				m_reverse	= reverse;
			}


			public NewStartOneTrackRequest(StepControl ctrl) : base(ctrl) { }


			protected override bool OnStart()
			{
				if (player.isPlaying)			// 재생중이면 계속 보류
					return false;


				ctrl.m_curTrackStep	= 0;		// 초기화
				ctrl.m_newTrackStep	= 0;
				ctrl.m_newTrackIsOn	= false;
				ctrl.m_lastMoveWasReversed	= false;

				var startstep	= m_reverse? (m_newTrack.sectionCount + 1) * 2 - 1 : 1;	// 시작은 자연진행으로 (홀수 스텝)
				var nextstep	= m_reverse? startstep - 1 : startstep + 1;				// 완전히 진행된 후의 스텝

				player.SetNewTrack(m_newTrack, null);
				var handle		= player.ProgressStepTo(startstep, m_reverse);
				handle.consumedDelegate	= () =>
					{
						ctrl.m_curTrackStep	= nextstep;
					};
				handle.transitionDelegate = () =>
					{
						Debug.Log("transition over - next step : " + nextstep);
						isComplete	= true;
					};

				return true;
			}
		}




		// Members

		MonoBehaviour		m_context;
		MasterPlayer		m_player;
		Queue<BaseRequest>	m_reqQueue	= new Queue<BaseRequest>();

		struct Command
		{
			public enum Type
			{
				StartWithOneTrack,
				StartWithTwoTrack,
				StepMove,
			}

			public Type	type;
			public object param1;
			public object param2;
			public object param3;
			public object param4;
			public object param5;
		}

		Queue<Command>		m_cmdQueue	= new Queue<Command>();	// 명령 큐 (MessageQueue 만으로는 해결되지 않는 명령 중첩 문제가 있어서)


		int					m_curTrackStep;			// 현재 트랙의 step
		int					m_newTrackStep;			// 새 트랙의 step
		bool				m_newTrackIsOn;			// 새 트랙이 현재 트랜지션중인지
		bool				m_lastMoveWasReversed;	// 가장 최근의 이동이 역방향 진행이었는지


		public StepControl(MasterPlayer player, MonoBehaviour context)
		{
			m_player	= player;
			m_context	= context;

			m_context.StartCoroutine(UpdateCo());
		}

		IEnumerator UpdateCo()
		{
			while (true)
			{
				yield return null;

				// 커맨드 큐에 들어온 명령어가 있고, 메세지 큐에 쌓인 메세지가 일정 수 이하일 때만 처리
				if (m_cmdQueue.Count > 0 && m_reqQueue.Count < 2)
				{
					var cmd	= m_cmdQueue.Dequeue();
					switch(cmd.type)
					{
						case Command.Type.StartWithOneTrack:
							_StartWithOneTrack((Data.Track)cmd.param1, (int)cmd.param2, (bool)cmd.param3);
							break;

						case Command.Type.StartWithTwoTrack:
							_StartWithTwoTrack((Data.Track)cmd.param1, (int)cmd.param2, (Data.Track)cmd.param3, (Data.TransitionScenario)cmd.param4, (bool)cmd.param5);
							break;

						case Command.Type.StepMove:
							_StepMove((int)cmd.param1, (Data.Track)cmd.param2, (Data.TransitionScenario)cmd.param3, (bool)cmd.param4);
							break;

						default:
							throw new System.InvalidOperationException("unknown command type " + cmd.type);
					}
				}

				if (m_reqQueue.Count > 0)
				{
					var req	= m_reqQueue.Peek();		// 가장 최근의 요청을 가져오고
					req.Start();						// 시작되지 않은 경우엔 시작
					req.Update();						// 업데이트 (이미 완료된 경우엔 실행되지 않음)

					if (req.isComplete)					// 완료된 요청은 제거한다.
					{
						m_reqQueue.Dequeue();
						Debug.Log("dequeue");
					}
				}
			}
		}



		/// <summary>
		/// 트랙 1개 재생 시작
		/// </summary>
		/// <param name="track"></param>
		public void StartWithOneTrack(Data.Track track, int startStep, bool reverse = false)
		{
			m_cmdQueue.Enqueue(new Command() { type = Command.Type.StartWithOneTrack, param1 = track, param2 = startStep, param3 = reverse });
		}

		void _StartWithOneTrack(Data.Track track, int startStep, bool reverse = false)
		{
			var newreq		= new NewStartOneTrackRequest(this);
			newreq.Setup(track, reverse);
			m_reqQueue.Enqueue(newreq);
		}

		/// <summary>
		/// 트랙 2개 재생 시작 (하나는 메인, 하나는 사이드)
		/// </summary>
		public void StartWithTwoTrack(Data.Track mainTrack, int startStep, Data.Track sideTrack, Data.TransitionScenario transcen, bool reverse = false)
		{
			m_cmdQueue.Enqueue(new Command() { type = Command.Type.StartWithTwoTrack, param1 = mainTrack, param2 = startStep, param3 = sideTrack, param4 = transcen, param5 = reverse });
		}

		void _StartWithTwoTrack(Data.Track mainTrack, int startStep, Data.Track sideTrack, Data.TransitionScenario transcen, bool reverse = false)
		{
			
		}

		/// <summary>
		/// 트랜지션 단계 진행
		/// </summary>
		/// <param name="reverse"></param>
		public void StepMove(int step, bool reverse = false)
		{
			StepMove(step, null, null, reverse);
		}

		/// <summary>
		/// 트랜지션 단계 진행. 새 트랙을 준비한다.
		/// </summary>
		/// <param name="newTrack"></param>
		/// <param name="transcen"></param>
		/// <param name="reverse"></param>
		public void StepMove(int step, Data.Track newTrack, Data.TransitionScenario transcen, bool reverse = false)
		{
			m_cmdQueue.Enqueue(new Command() { type = Command.Type.StepMove, param1 = step, param2 = newTrack, param3 = transcen, param4 = reverse });
		}

		void _StepMove(int step, Data.Track newTrack, Data.TransitionScenario transcen, bool reverse = false)
		{
			//if ((m_newTrackIsOn? m_newTrackStep : m_curTrackStep) == step)
			Debug.Log(string.Format("m_curTrackStep : {0}, step : {1}", m_curTrackStep, step));
			if (m_curTrackStep == step)				// 동일한 step으로 진행하는 요청이 들어온 경우엔 무시한다.
			{
				Debug.LogWarning("same step");
				return;
			}

			// 현재 진행중인 요청이 stepmove고, 한번 더 진행이 가능한 경우에는 따로 요청을 늘리지 않는다. (자연 -> 강제 전환으로 바꾸는 것임)
			var stepMoveReq	= m_reqQueue.Count > 0 ? m_reqQueue.Peek() as StepMoveRequest : null;
			if (stepMoveReq != null && stepMoveReq.MoveOnceMore(reverse))
			{
				return;
			}

			var newreq		= new StepMoveRequest(this);
			//var newstep		= m_newTrackIsOn? m_newTrackStep : m_curTrackStep;
			//newstep			+= reverse? -1 : 1;
			newreq.Setup(step, reverse, newTrack, transcen);

			m_reqQueue.Enqueue(newreq);
		}
	}
}