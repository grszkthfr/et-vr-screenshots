using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;
using SMI;
//using System.Text
using System;

// TODO: do not overwrite file! + cleaning up and commenting + direction head is looking to

public class CaptureScreenshotWithGaze : MonoBehaviour
{

    //GameObject for gaze cursor
    GameObject gazeVis = null;
    Vector3 initialeScale = Vector3.zero;

    SMI.SMIEyeTrackingUnity smiInstance = null;

    public int subject_id = 10;
    string video_id;
    int video_frame_n = 0;
    int video_frame_max = 0;
    int hmd_frame = 0;
    Vector3 screenPos;
    Vector3 camAngle;


    //Für TextWriter Eye Tracking
    TextWriter sw;

    public SMIEyeTrackingUnity smi;
    public Camera cam;

    readonly VideoClip videoClip; // old:     UnityEngine.Video.VideoClip videoClip;

    string log_header;
    string frame_log;

    static string date = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd");

    private int captureWidth = 756;     // 4k = 3840 x 2160´, 1080p = 1920 x 1080, 1512 x 1680, 756 x 840
    private int captureHeight = 840;
 
    // optimize for many screenshots will not destroy any objects so future screenshots will be fast
    private bool optimizeForManyScreenshots = true; // necessary or make default?

    private Rect rect;
    private RenderTexture renderTexture;
    private Texture2D screenShot;

    // commands
    private bool captureScreenshot = false;
   
   void Awake()
   {
       
       //Kalibrieren();
       
    }
    
    void Start() 
    {

        Debug.Log("Create log file");

        //get video_id from video player
        var videoPlayer = GameObject.Find("360sphere").GetComponent<UnityEngine.Video.VideoPlayer>();
        video_id = videoPlayer.clip.name;
        video_frame_max = unchecked((int)videoPlayer.clip.frameCount);
        Debug.Log("video_id is: " + video_id + "\nmax_frame_video is: " + video_frame_max);

        smiInstance = SMIEyeTrackingUnity.Instance;

            gazeVis = (GameObject)Resources.Load("SMI_GazePoint");
            if (gazeVis != null)
            {
                gazeVis = Instantiate(gazeVis, Vector3.zero, Quaternion.identity) as GameObject;
                gazeVis.name = "SMI_GazePoint";
                initialeScale = gazeVis.transform.localScale;
            }
            else
            {
                Debug.LogError("Unity Prefab missing: SMI_GazePoint");
                UnityEditor.EditorApplication.isPlaying = false;

        }

        // prepare log
        string log_file = "";
        // path saving log files
        string log_path = Directory.GetCurrentDirectory() + "/log" + "/subject_" + subject_id.ToString("D2");
        // path saving screenshots
        string screenshot_path = log_path + "/frames/" + video_id;

        // check path exists
        if (!Directory.Exists(log_path))
            // create folder if it does not exist
            {
                Directory.CreateDirectory(log_path);
            }

        if (!Directory.Exists(screenshot_path))
            // create folder if it does not exist
            {
            Directory.CreateDirectory(screenshot_path);
            }
        else
           {
           Debug.LogError("Directory for frames already exists! Please check subject_id or scene.");
           UnityEditor.EditorApplication.isPlaying = false;
           }



        log_file = log_path + "/log_" +  date + '_' + subject_id.ToString("D2") + "_" + video_id + ".txt";
        // Debug.Log(log_file);

        // Debug.Log("Opening log_file...");
        sw = new StreamWriter(log_file);//, true); // append = true, don't overwrtie log file

        log_header = "subject_id" + "\t" + "date" + "\t" + "video_id" +
                        "\t" + "frame_video" + 
                        "\t" + "screenPos.x" + "\t" + "screenPos.y" +
                        "\t" + "camAngle.x" + "\t" + "camAngle.y" + "\t" + "camAngle.z"; 


        // Debug.Log("Writing header...");
        sw.WriteLine(log_header);
        
    }
    
    public void CaptureScreenshot()
    {
        captureScreenshot = true; //necessary to evaluate?!
    }

    // Update is called once per frame
    void Update() 
    {
        var videoPlayer = GameObject.Find("360sphere").GetComponent<UnityEngine.Video.VideoPlayer>(); // warum 2 mal? einmal in update einmal in awake
        // path saving log files
        string log_path = Directory.GetCurrentDirectory() + "/log" + "/subject_" + subject_id.ToString("D2");
        // path saving screenshots
        string screenshot_path = log_path + "/frames/" + video_id; // warum 2 mal? einmal in update einmal in awake

        if (gazeVis != null)
            UpdateThePosition();

        // count frame
        hmd_frame += 1; // (VideoPlayer.frame)
        video_frame_n = unchecked((int)videoPlayer.frame); // https://stackoverflow.com/questions/858904/can-i-convert-long-to-int
        //Debug.Log("frame_n: " + frame_hmd + "\nframe_video: " + frame_video);

        // coords on screen
        //"Screenspace is defined in pixels. The bottom-left of the screen is
        // (0,0); the right-top is (pixelWidth,pixelHeight). The z position is
        // in world units from the camera."
        screenPos = cam.WorldToScreenPoint(gazeVis.transform.position);
        camAngle = cam.transform.localEulerAngles;

        ////////////////////////////////////////////////////////////////////////

        frame_log = subject_id.ToString("D2") + "\t" + date + "\t" + video_id + "\t" + video_frame_n.ToString("D5")
                        + "\t" + Math.Round(screenPos.x) + "\t" + Math.Round(screenPos.y)
                        + "\t" + Math.Round(camAngle.x) + "\t" + Math.Round(camAngle.y) + "\t" + Math.Round(camAngle.z);
                        // + "\t" + bi_gaze_dir.z  
                        // + "\t" + right_gaze_dir.x + "\t" + right_gaze_dir.y  
                        // + "\t" + right_gaze_dir.z  
                        // + "\t" + left_gaze_dir.x + "\t" + left_gaze_dir.y  
                        // + "\t" + left_gaze_dir.z; 

        sw.WriteLine(frame_log);

        ////////////////////////////////////////////////////////////////////////

        captureScreenshot |=
            video_frame_n >= 230  & // don't capture screenshots when video is not playing; 250 frames black (50fps * 5 sekunde) - 20 = 3 black screenshots, frame 250 last black
            video_frame_n % 10 == 0 & // capture every 10th screenshot
            video_frame_n <= video_frame_max-60; // don't capture screenshots after last frame;  100 frames black (50fps * 2 sekunde) - 60 = 4 black screenshots, frame 1000 last color

        if (captureScreenshot)

        {

            Debug.Log("Take screen shot: " + video_frame_n.ToString("D5"));

            captureScreenshot = false;

            // create screenshot objects if needed
            if (renderTexture == null)
            {
                // creates off-screen render texture that can rendered into
                rect = new Rect(0, 0, captureWidth, captureHeight);
                renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
                screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }

            // get main camera and manually render scene into renderTexture
            Camera camera = this.GetComponent<Camera>(); // NOTE: added because there was no reference to camera in original script; must add this script to Camera
            camera.targetTexture = renderTexture;
            camera.Render();
 
            // read pixels will read from the currently active render texture so make our offscreen 
            // render texture active and then read the pixels
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(rect, 0, 0);
 
            // reset active camera texture and render texture
            camera.targetTexture = null;
            RenderTexture.active = null;

            // get filename
            string screenshot_filename = screenshot_path + "/" + video_id + "_frame_" + video_frame_n.ToString("D5") + ".ppm";

            // pull in our file header/data bytes for the specified image format (has to be done from main thread)
            byte[] screenshot_header = null;
            byte[] screenshot_data = null;
                      
            // create a file header for ppm file
            string screenshot_headerstring = string.Format("P6\n{0} {1}\n255\n", rect.width, rect.height);
            screenshot_header = System.Text.Encoding.ASCII.GetBytes(screenshot_headerstring);
            screenshot_data = screenShot.GetRawTextureData();
             
            // create new thread to save the image to file (only operation that can be done in background)
            new System.Threading.Thread(() =>
            {
                // create file and write optional header with image bytes
                var f = System.IO.File.Create(screenshot_filename);
                if (screenshot_header != null) f.Write(screenshot_header, 0, screenshot_header.Length);
                f.Write(screenshot_data, 0, screenshot_data.Length);
                f.Close();
                //Debug.Log(string.Format("Wrote screenshot {0} of size {1}", screenshot_filename, screenshot_data.Length));
            }).Start();
 
            // cleanup if needed
            if (optimizeForManyScreenshots == false)
            {

                Destroy(renderTexture);
                renderTexture = null;
                screenShot = null;
            }
         }
        // sw.Close(); // needed?
    }



    public void Kalibrieren()
    {
        //Kalibrieren
        smi.smi_ResetCalibration();
        smi.smi_StartFivePointCalibration();
        // smi.smi_StartNumericalValidation();
        // if (Input.GetKeyDown(KeyCode.Space)) { smi.smi_StartFivePointCalibration(); } // maybe easiest to just start calibration by hand!
        // if (Input.GetKeyDown(KeyCode.V)) { smi.smi_StartNumericalValidation(); }
        // if (Input.GetKeyDown(KeyCode.T)) { Time.timeScale = 1; counting = true; HideView.SetActive(false); }
        // if (counting) { counter++; }
    }

    public void EndWrite()
    {
        Debug.Log("Closing...");
        sw.Close();
    }

    ////////////////////////////////////////////////////////////////////////////
    private void UpdateThePosition()
        {

            gazeVis.SetActive(true);

            RaycastHit hitInformation;

            //Get raycast from gaze
            smiInstance.smi_GetRaycastHitFromGaze(out hitInformation);
            if (hitInformation.collider != null)
            {
                gazeVis.transform.position = hitInformation.point;
                gazeVis.transform.localRotation = smiInstance.transform.rotation;
                gazeVis.transform.localScale = initialeScale * hitInformation.distance;
                gazeVis.transform.LookAt(Camera.main.transform);
                gazeVis.transform.transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
            else
            {
                //If the raycast does not collide with any object, put it far away.
                float distance = 100;
                Vector3 scale = initialeScale * distance;
                Ray gazeRay = smiInstance.smi_GetRayFromGaze();
                
                gazeVis.transform.position = gazeRay.origin + Vector3.Normalize(gazeRay.direction) * distance;
                gazeVis.transform.rotation = smiInstance.transform.rotation;
                if (gazeRay.direction != Vector3.zero)
                    gazeVis.transform.localScale = scale;
                else
                    gazeVis.transform.localScale = Vector3.zero;
            }

            //Toggle the gaze cursor by Key "g"
            if (Input.GetKeyDown(KeyCode.G))
            {
                gazeVis.GetComponent<MeshRenderer>().enabled = !(gazeVis.GetComponent<MeshRenderer>().enabled);
            }

        }
    void OnDisable()
    {
        EndWrite();
        Debug.Log("...done!");
    }
    ////////////////////////////////////////////////////////////////////////////
}
