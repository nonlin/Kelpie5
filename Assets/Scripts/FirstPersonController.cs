using UnityEngine;
using UnitySampleAssets.CrossPlatformInput;
using UnitySampleAssets.Utility;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnitySampleAssets.Characters.FirstPerson
{
    //[RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {

        //////////////////////// exposed privates ///////////////////////
        [SerializeField] private bool _isWalking;
		[SerializeField] private bool _isCrouching;
        [SerializeField] private float walkSpeed;
        [SerializeField] private float runSpeed;
		[SerializeField] private float crouchSpeed;
        [SerializeField] [Range(0f, 1f)] private float runstepLenghten;
        [SerializeField] private float jumpSpeed;
        [SerializeField] private float stickToGroundForce;
        [SerializeField] private float _gravityMultiplier;
        [SerializeField] private MouseLook _mouseLook;
        [SerializeField] private bool useFOVKick;
        [SerializeField] private FOVKick _fovKick = new FOVKick();
        [SerializeField] private bool useHeadBob;
        [SerializeField] private CurveControlledBob _headBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob _jumpBob = new LerpControlledBob();
        [SerializeField] private float _stepInterval;
        [SerializeField] private AudioClip breathingFastSound; // the sound played when running 
        [SerializeField] private AudioClip[] _footstepSounds; // an array of footstep sounds that will be randomly selected from.

        [SerializeField] private AudioClip _jumpSound; // the sound played when character leaves the ground.
        [SerializeField] private AudioClip _landSound; // the sound played when character touches back on ground.

		NetworkManager NM;
		public Animator anim;
		[SerializeField] Animator animMainCam;
		[SerializeField] Animator animEthan;
		[SerializeField] Animator animHitBoxes;
        private IEnumerator StaminaRegenCoroutine;
		GUIManager guiMan;
        ///////////////// non exposed privates /////////////////////////
        private Camera _camera;
        private bool _jump;
        private float _yRotation;
        private CameraRefocus _cameraRefocus;
        private Vector2 _input;
        private Vector3 _moveDir = Vector3.zero;
        private CharacterController _characterController;
        private CollisionFlags _collisionFlags;
        private bool _previouslyGrounded;
        private Vector3 _originalCameraPosition;
        private float _stepCycle = 0f;
        private float _nextStep = 0f;
        private bool _jumping = false;
		private float stamina = 100f;
		//
		private Vector3 moveInput;
		private float turnAmount;
		private float forwardAmount;
		private Vector3 camForward; // The current forward direction of the camera
		private Vector3 move;
		private Transform cam; // A reference to the main camera in the scenes transform
		float mouseTempX;
		float mouseTempY; 
		float staminaDrain;
		float staminaRecover;
        //[SerializeField] GameObject crosshair; 
        public GameObject[] weapons;
        public int currentWeapon;
        public int previousWeapon;
        public bool previousBool = false;
        public PlayerNetworkMover PNM;
        public PlayerShooting playerShooting;
		void Awake(){

            staminaDrain = 0.9f;
            staminaRecover = 0.5f;
            //Init GameObjects and Scripts before creating this game object
			guiMan = GameObject.Find ("NetworkManager").GetComponent<GUIManager> ();
			NM = GetComponent<NetworkManager> ();
            //PNM = this.GetComponent<PlayerNetworkMover>();
            //PNM.currentWeapon = weapons[0].GetComponent<Weapon>();
            //playerShooting.UpdateAmmoText();
            //playerShooting = GameObject.FindGameObjectWithTag("WeaponsCam").GetComponent<PlayerShooting>();
            //Enable crosshair when spawning player if it isn't null which it might be from WinPrompt
            if (GameObject.Find("Crosshair") != null)
                GameObject.Find("Crosshair").GetComponent<RawImage>().enabled = true;
            //Doesn't differentiate between other players
            //anim = GameObject.FindGameObjectWithTag("WeaponMain").GetComponent<Animator>();

		}
        // Use this for initialization
        private void Start()
        {
            currentWeapon = 0;
            previousWeapon = 0;
            _characterController = GetComponent<CharacterController>();
            _camera = Camera.main;
            _originalCameraPosition = _camera.transform.localPosition;
            _cameraRefocus = new CameraRefocus(_camera, transform, _camera.transform.localPosition);
            _fovKick.Setup(_camera);
            _headBob.Setup(_camera, _stepInterval);
            _stepCycle = 0f;
            _nextStep = _stepCycle/2f;
            _jumping = false;
            gameObject.GetComponents<AudioSource>()[3].clip = breathingFastSound;
            NM = GameObject.Find("NetworkManager").GetComponent<NetworkManager>(); 
			_mouseLook.XSensitivity = PlayerPrefs.GetFloat ("xAxis");
			_mouseLook.YSensitivity = PlayerPrefs.GetFloat ("yAxis");
			_mouseLook.smooth = (PlayerPrefs.GetInt("smooth") != 0);
			mouseTempX = _mouseLook.XSensitivity;
			mouseTempY = _mouseLook.YSensitivity;

			//players = GameObject.FindGameObjectsWithTag("Player");
			// get the transform of the main camera
			if (Camera.main != null)
			{
				cam = Camera.main.transform;
			}
			else
			{
				Debug.LogWarning(
					"Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.");
				// we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
			}
            //initlize a weapon when spawned default 0 (AK)
            SelectWeapon(0);
		}
		
		// Update is called once per frame
		private void Update()
		{
			
			RotateView();
			// the jump state needs to read here to make sure it is not missed
            if (!_jump)
                _jump = CrossPlatformInputManager.GetButtonDown("Jump");

            if (!_previouslyGrounded && _characterController.isGrounded)
            {
                StartCoroutine(_jumpBob.DoBobCycle());

				NM.player.GetComponent<PhotonView>().RPC("PlayLandingSound",PhotonTargets.All);
                //PlayLandingSound();
                _moveDir.y = 0f;
                _jumping = false;
            }
            if (!_characterController.isGrounded && !_jumping && _previouslyGrounded)
            {
                _moveDir.y = 0f;
            }

            _previouslyGrounded = _characterController.isGrounded;
			//TabMenu ();
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = _camera.transform.forward*_input.y + _camera.transform.right*_input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo,
                               _characterController.height/2f);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            _moveDir.x = desiredMove.x*speed;
            _moveDir.z = desiredMove.z*speed;


            if (_characterController.isGrounded)
            {
                _moveDir.y = -stickToGroundForce;

                if (_jump)
                {
                    _moveDir.y = jumpSpeed;
	
					NM.player.GetComponent<PhotonView>().RPC("PlayJumpSound",PhotonTargets.All);
                    //PlayJumpSound();
                    _jump = false;
                    _jumping = true;
                }
            }
            else
            {
                _moveDir += Physics.gravity*_gravityMultiplier;
            }

            _collisionFlags = _characterController.Move(_moveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

	
		}
		
		
		private void ProgressStepCycle(float speed)
		{
			if (_characterController.velocity.sqrMagnitude > 0 && (_input.x != 0 || _input.y != 0))
                _stepCycle += (_characterController.velocity.magnitude + (speed*(_isWalking ? 1f : runstepLenghten)))*
                              Time.fixedDeltaTime;

            if (!(_stepCycle > _nextStep)) return;

            _nextStep = _stepCycle + _stepInterval;

			NM.player.GetComponent<PhotonView>().RPC("PlayFootStepAudio",PhotonTargets.All);
            //PlayFootStepAudio();
        }



        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!useHeadBob) return;
            if (_characterController.velocity.magnitude > 0 && _characterController.isGrounded)
            {
                _camera.transform.localPosition =
                    _headBob.DoHeadBob(_characterController.velocity.magnitude +
                                       (speed*(_isWalking ? 1f : runstepLenghten)));
                newCameraPosition = _camera.transform.localPosition;
                newCameraPosition.y = _camera.transform.localPosition.y - _jumpBob.Offset();
            }
            else
            {
                newCameraPosition = _camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - _jumpBob.Offset();
            }
            _camera.transform.localPosition = newCameraPosition;

            _cameraRefocus.SetFocusPoint();
        }

        public void WeaponInput() {
             
            if (Input.GetAxis("Mouse ScrollWheel") > 0) {

                if (currentWeapon + 1 <= weapons.Length-1)
                {
                    currentWeapon++;
                }//loop back to start of list when scrolling up/forward
                else { currentWeapon = 0; }
                SelectWeapon(currentWeapon);
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0) {

                if (currentWeapon - 1 >= 0)
                {
                    currentWeapon--;
                }//Loop back to end of list when scrolling down/backwards
                else { currentWeapon = weapons.Length-1; }
                SelectWeapon(currentWeapon);

            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                //Prevents setting currentWeapon = previous weapon if user hits the same weapon twice
                if (currentWeapon != 0)
                    previousWeapon = currentWeapon;
                currentWeapon = 0;
                SelectWeapon(currentWeapon);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (currentWeapon != 1)
                    previousWeapon = currentWeapon;
                currentWeapon = 1;
                SelectWeapon(currentWeapon);
            }

            if (Input.GetKeyDown(KeyCode.Q)) {

                //Swaps to last weapon used
                if (!previousBool)
                {
                    previousBool = !previousBool;
                    SelectWeapon(previousWeapon);
                }
                else {
                    previousBool = !previousBool;
                    SelectWeapon(currentWeapon); 
                }
            }
            
        }

        public void SelectWeapon(int index) {

            Debug.Log("Weapon Index is " + index);
            for (int i = 0; i < weapons.Length; i++) {

                if (i == index)
                {
                    //Update Weapon animations and models for weapon switch
                    weapons[i].SetActive(true);
                    anim = weapons[i].GetComponent<Animator>();
                    //Update Player Network Mover for Weapon Switwch
                    PNM.anim = weapons[i].GetComponent<Animator>();
                    PNM.currentWeapon = weapons[i].GetComponent<Weapon>();
                    PNM.currentWeaponIndex = index;
                    PNM.updateWeaponIndex = true;
                    //To update the weapon layers again each weapon switch
                    PNM.WeaponSetup();
                    //Update Player Shooting for Weapon Switch
                    playerShooting.WeaponStats = weapons[i].GetComponent<Weapon>();
                    playerShooting.UpdateAmmoText();
                    //To assign each weapon the proper type of muzzle flash
                    playerShooting.muzzleFlash = weapons[i].GetComponentInChildren<ParticleSystem>();
                    playerShooting.AssignMuzzleLightSource();
                }
                else { weapons[i].SetActive(false); }
            }
        }

        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");
			bool doOnce = false;
			bool waswalking = _isWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            _isWalking = !Input.GetKey(KeyCode.LeftShift) || stamina <= staminaDrain;
           
			bool aim = false;
			if(_isWalking)
				aim = Input.GetButton("Fire2");
			_isCrouching = Input.GetKey(KeyCode.LeftControl);

            //Need this hear to set animations to proper weapon
            bool qSwapping = Input.GetKeyDown(KeyCode.Q);
            if (qSwapping) 
            {

                //SwapWeapons();
            }

            WeaponInput();

			//Slow mouse movement when aiming by 45 percent
			if(aim && !doOnce){
				_isWalking = true; 
				doOnce = true;
                if(GameObject.Find("Crosshair") != null)
                    GameObject.Find("Crosshair").GetComponent<RawImage>().enabled = false;
				_mouseLook.XSensitivity = mouseTempX - (mouseTempX * 0.45f);
				_mouseLook.YSensitivity = mouseTempY - (mouseTempY  * 0.45f);

				Debug.Log (_mouseLook.YSensitivity);
				
			}
            //Return Mouse movement values to default
            if (!aim)
            {
                doOnce = false;
                _mouseLook.XSensitivity = PlayerPrefs.GetFloat("xAxis");
                _mouseLook.YSensitivity = PlayerPrefs.GetFloat("yAxis");
                if (GameObject.Find("Crosshair") != null) { 
                    GameObject.Find("Crosshair").GetComponent<RawImage>().enabled = true;
                }
			}
			//Player Movement Animation Logic
			AnimationLogic(vertical, horizontal);

			//Weapon Animations
			anim.SetBool("Sprint", !_isWalking);
			anim.SetBool ("Aim", aim);

            //Drain Stamina if we are sprinting and moving
            if (!_isWalking && _characterController.velocity != Vector3.zero && stamina >= staminaDrain)
            {// && _characterController.velocity.x > 0.1f){
                stamina = stamina - staminaDrain;
                useHeadBob = true;
                
                //Debug.Log(stamina);
            }
            else if(_isWalking && useHeadBob) {
                //if sound not playing play it
                if (!gameObject.GetComponents<AudioSource>()[3].isPlaying)
                {
                    gameObject.GetComponents<AudioSource>()[3].Play();
                }
                useHeadBob = false;
                //gameObject.GetComponents<AudioSource>()[3].Stop();
            }
            //Stamina Regen conditions, regen only when they are not trying to run
            if (stamina <= 10  && !Input.GetKey(KeyCode.LeftShift) && !gameObject.GetComponents<AudioSource>()[3].isPlaying) {
                StaminaRegenCoroutine = StaminaRegen(3.0f);
                StartCoroutine(StaminaRegenCoroutine);
            }
            else if (stamina > 10 && stamina < 50 && !Input.GetKey(KeyCode.LeftShift))
            {
                StaminaRegenCoroutine = StaminaRegen(2.5f);
                StartCoroutine(StaminaRegenCoroutine);
            }
            else if (stamina >= 50 && stamina < 100 && !Input.GetKey(KeyCode.LeftShift))
            {
                StaminaRegenCoroutine = StaminaRegen(1.5f);
                StartCoroutine(StaminaRegenCoroutine);
            }
#endif
            // set the desired speed to be walking or running
            speed = walkSpeed;

			if (!_isWalking) {
			
				speed = runSpeed;
			}
			if (_isCrouching) {
				speed = crouchSpeed;			
			}
            //speed = _isWalking ? walkSpeed : runSpeed;
            _input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (_input.sqrMagnitude > 1) _input.Normalize();

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (_isWalking != waswalking && useFOVKick && _characterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!_isWalking ? _fovKick.FOVKickUp() : _fovKick.FOVKickDown());
            }
        }

        private void RotateView()
        {
            Vector2 mouseInput = _mouseLook.Clamped(_yRotation, transform.localEulerAngles.y);

            // handle the roation round the x axis on the camera
            _camera.transform.localEulerAngles = new Vector3(-mouseInput.y, _camera.transform.localEulerAngles.y,
                                                             _camera.transform.localEulerAngles.z);
            _yRotation = mouseInput.y;
            transform.localEulerAngles = new Vector3(0, mouseInput.x, 0);
            _cameraRefocus.GetFocusPoint();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic)
                return;

            //dont move the rigidbody if the character is on top of it
            if (_collisionFlags == CollisionFlags.CollidedBelow) return;

            body.AddForceAtPosition(_characterController.velocity*0.1f, hit.point, ForceMode.Impulse);

        }


		void OnGUI(){

			if (Input.GetKey(KeyCode.Tab)) {

				guiMan.ScoreBoard();
			}
		}

		void AnimationLogic(float vertical, float horizontal){

			//Walking Animation Logic Forward //If we aren't walking we want to set it to 0 to stop animations from continuing to play
			//Here we have to hard code values for .5 because 1 would mean we are running// .5 is for walking 1 is for running for all movements
			if((_isWalking || _isCrouching) && vertical > 0 ){
				animHitBoxes.SetBool("Moving",true);
				animEthan.SetFloat("Forward", 0.5f);
			}
			else if(!_isWalking && vertical > 0 ){
				animHitBoxes.SetBool("Moving",true);
				animEthan.SetFloat("Forward", vertical);
			}
			else{
				animEthan.SetFloat("Forward", vertical);
				animHitBoxes.SetBool("Moving",false);
			}
			//Backwards
			if(_isWalking && vertical < 0 ){
				animHitBoxes.SetBool("Moving",true);
				animEthan.SetFloat("Forward", -0.5f);
			}
			else if(!_isWalking && vertical < 0 ){
				animHitBoxes.SetBool("Moving",true);
				animEthan.SetFloat("Forward", vertical);
			}
			//else{animHitBoxes.SetBool("Moving",false);}

			//Turning Right
			if(_isWalking && horizontal > 0 )
				animEthan.SetFloat("Turn", 0.5f);
			else if(!_isWalking && vertical > 0 )
				animEthan.SetFloat("Turn", horizontal);
			//Turning Left
			if(_isWalking && horizontal < 0 )
				animEthan.SetFloat("Turn", -0.5f);
			else if(!_isWalking && vertical < 0 )
				animEthan.SetFloat("Turn", horizontal);
			else
				animEthan.SetFloat("Turn", horizontal);

			//Jumping
			animEthan.SetBool("OnGround",_characterController.isGrounded);
			if (!_characterController.isGrounded && horizontal < 0) {
				animEthan.SetFloat("JumpLeg", -0.5f);
			}
			else if(!_characterController.isGrounded && horizontal > 0) {
				animEthan.SetFloat("JumpLeg", 0.5f);
			}
			else{
				animEthan.SetFloat("JumpLeg", 0);
			}
			if (!_characterController.isGrounded) {
				animEthan.SetFloat("Jump",5 - 0.1f);			
			}

            //Crouching
            //When Crouching we have to toggle the characters animations, shift the hit boxes and shift the weapons position to have a convincing looking crouch
            if (_isCrouching)
            {

                animMainCam.SetBool("Crouch", _isCrouching);
                animHitBoxes.SetBool("Crouch", _isCrouching);
                animEthan.SetBool("Crouch", _isCrouching);
            }
            else
            {
                animMainCam.SetBool("Crouch", _isCrouching);
                animHitBoxes.SetBool("Crouch", _isCrouching);
                animEthan.SetBool("Crouch", _isCrouching);
            }

			
			//animEthan.SetBool ("Crouch",_isCrouching);
		}
		IEnumerator StaminaRegen(float waitTime){

            yield return new WaitForSeconds(waitTime); 

			if(_isWalking && stamina <= 100){
				stamina = stamina + staminaRecover;
				Debug.Log (stamina);
			}
		}
    }
}
