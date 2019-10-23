# importing the packages
from pyimagesearch.tempimage import TempImage
from picamera.array import PiRGBArray
from picamera import PiCamera
import argparse
import warnings
import datetime
import dropbox
import imutils
import json
import time
import cv2

##TODO Install imutils onto Pi ##

#argument parser and parse arguments
ap = argparse.ArgumentParser()
ap.add_argument("-c", "--conf", required=True,
	help="path to the JSON configuration file")
args = vars(ap.parse_args())

#filter the warnings, load the config and initialse the dropbox client
warnings.filterwarnings("ignore")
conf = json.load(open(args["conf"]))
client = None

#check if dropbox is being used TODO: change to google drive later
if conf["use_dropbox"]:
    #client = dropbox.Dropbox(conf["dropbox_access_token"])
    print("[SUCCESS] dropbox account linked")

#Get the camera ready
camera = PiCamera()
camera.resolution = tuple(conf["resolution"])
camera.framerate = conf["fps"]
rawCapture = PiRGBArray(camera, size=tuple(conf["resolution"]))

## allow the camera to warm up
print("[INFO] warming up...")
time.sleep(conf["camera_warmup_time"])
# initialise the average frame
avg = None
# Last uploaded timestamp
lastUploaded = datetime.datetime.now()
#Frame motion counter
motionCounter = 0

# Capture frames from camera
for f in camera.capture_continuous(rawCapture, format="bgr", use_video_port=True):
    # get the raw NumPy array representing the image and initialize
	# the timestamp and occupied/unoccupied text
    frame = f.array
    timestamp = datetime.datetime.now()
    text = "Unoccupied"

    #resize the frame, convert it to grayscale and blur it for camera vission
    frame = imutils.resize(frame, width=500)
    grey = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    grey = cv2.GaussianBlur(grey, (21, 21), 0)

    #if avg frame is none: initialise it
    if avg is None:
        print("[INFO] starting background model...")
        avg = grey.copy().astype("float")
        rawCapture.truncate(0)
        continue
    
    
    # accumulate the weighted average between the current frame and
	# previous frames, then work out the difference between the current
	# frame and average
    cv2.accumulateWeighted(grey, avg, 0.5)
    frameDelta = cv2.absdiff(grey, cv2.convertScaleAbs(avg))

    # threshold the delta image, dilate the thresholded image to fill
	# in holes, then find contours on thresholded image
    thresh = cv2.threshold(frameDelta, conf["delta_thresh"], 255, cv2.THRESH_BINARY)[1]
    thresh = cv2.dilate(thresh, None, iterations=2)
    cnts = cv2.findContours(thresh.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cnts = imutils.grab_contours(cnts)

    # Loop over the contours
    for c in cnts:
        # if the contour is too small: ignore
        if cv2.contourArea(c) < conf["min_area"]:
            continue

        # Work out the bounding box for the contour, draw it on the frame,
		# and update the text
        (x, y, w, h) = cv2.boundingRect(c)
        cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
        text = "Ocupied"

    #Draw the text and timestamp on the frame
    ts = timestamp.strftime("%A %d %B %Y %I :%M:%S%p")
    cv2.putText(frame, "Room Status: {}".format(text), (10, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 2)
    cv2.putText(frame, ts, (10, frame.shape[0] - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (0, 0, 255), 1)

    # Check if room is ocupied
    if text == "Ocupied":
        # check to see if enough time has passed between uploads
        if (timestamp - lastUploaded).seconds >= conf["min_upload_seconds"]:
            #increment the motion counter
            motionCounter += 1

            # check to see if the number of frames with consistent motion is
            # high enough
            if motionCounter >= conf["min_motion_frames"]:
                # check to see if drop box should be used
                if conf["use_dropbox"]:
                    #write the image to temp file
                    t = TempImage()
                    cv2.imwrite(t.path, frame)

                    # Upload the image to dropbox and clean up temp file
                    print("[UPLOAD] {}".format(ts))
                    path = "/{base_path}/{timestamp}.jpg".format(base_path=conf["dropbox_base_path"], timestamp=ts)
                    client.files_upload(open(t.path, "rb").read(), path)
                    t.cleanup()

                #Upload last uploaded timestamp and reset motion counter
                lastUploaded = timestamp
                motionCounter = 0

    # or the room is not ocupied
    else:
        motionCounter = 0

    #check if the frame should be displayed on screen
    if conf["show_video"]:
        #display feed
        cv2.imshow("Security Feed", frame)
        key = cv2.waitKey(1) & 0xFF

        #if the 'q' key is pressed, break from the loop
        if key == ord("q"):
            break

    # clear the stream in preparation for the next frame
    rawCapture.truncate(0)


