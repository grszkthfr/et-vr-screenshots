# !/usr/bin/env python
# -*- coding: utf-8 -*- #

import os
import cv2 as cv2
from moviepy.video.io.ffmpeg_tools import ffmpeg_extract_subclip, ffmpeg_merge_video_audio
import numpy as np

# directories
org_dir = "original_vids/"
val_dir = "validation_vids/"

# validation points
pois = [
        # top row from left to right
        ["a",(480,720)],["b",(960,720)],["c",(1440,720)],
        ["d",(2400,720)],["e",(2880,720)],["f",(3360,720)],
        # bottom row from left to right
        ["A",(480,1200)],["B",(960,1200)],["C",(1440,1200)],
        ["D",(2400,1200)],["E",(2880,1200)],["F",(3360,1200)]
        ]

# font characteristics
font                   = cv2.FONT_HERSHEY_SIMPLEX
fontScale              = 3
fontColor              = (128,128,128)
lineType               = 3

# for each video in dir_org
for vid in os.listdir(org_dir):
    #print (vid)

    # find input video
    org_vid = cv2.VideoCapture(org_dir + os.path.sep + vid)

    # define the codec and create VideoWriter object
    fourcc = cv2.VideoWriter_fourcc(*'XVID')
    val_vid = cv2.VideoWriter(val_dir + os.path.sep + "val_" + vid, fourcc, 50.0, (3840,1920))

    success, frame = org_vid.read()
    count = 1

    while (org_vid.isOpened()):
        success, frame = org_vid.read()
        count += 1

        if success==True:

            for poi in pois:
                #print(poi)

                cv2.putText(frame, poi[0], 
                    poi[1], 
                    font, 
                    fontScale, 
                    fontColor, 
                    lineType)
                cv2.circle(frame, poi[1], 15, (147,20,255), 10)

            # BGR: OpenCV represents images as NumPy arrays in reverse order
            # cv2.circle(frame, (0,0), 15, (255,0,0), 10)
            # cv2.circle(frame, (1920,0), 15, (255,0,0), 10)
            # cv2.circle(frame, (3840,0), 15, (255,0,0), 10)

            # cv2.circle(frame, (3840,1920), 15, (0,0,255), 10)
            # cv2.circle(frame, (1920,1920), 15, (0,0,255), 10)
            # cv2.circle(frame, (0,1920), 15, (0,0,255), 10)

            cv2.circle(frame, (1920,960), 15, (0,255,0), 10)
            cv2.putText(frame, 'north', (1920,960), font, fontScale, fontColor,
                lineType)

            cv2.circle(frame, (960,960), 15, (255,0,0), 10)
            cv2.putText(frame, 'west', (960,960), font, fontScale, fontColor,
                lineType)

            cv2.circle(frame, (2880,960), 15, (0,0,255), 10)
            cv2.putText(frame, 'east', (2880,960), font, fontScale, fontColor,
                lineType)

            cv2.circle(frame, (0,960), 15, (255,0,0), 10)
            cv2.circle(frame, (3840,960), 15, (0,0,255), 10)
            cv2.putText(frame, 'south', (0,960), font, fontScale, fontColor,
                lineType)

            if not os.path.isfile(val_dir + os.path.sep + "val_" + vid[:3]+".png"):
                cv2.imwrite(val_dir + os.path.sep + "val_" + vid[:3]+".png",
                    frame)

            if count % 100 == 0:
                print("Write frame of %s: %d" % (vid, count))

            # write the frame with circles
            val_vid.write(frame)

        else:
            break

print("Done.")