import numpy as np
import cv2
import time
import random
from enum import Enum
from mousesten import move_to
from screensten import grab_screen

def main():

    for i in list(range(4))[::-1]:
        print(i+1)
        time.sleep(1)

    region_x1 = 600
    region_y1 = 200
    region_x2 = 1320
    region_y2 = 500

    class States(Enum):
        NOT_FISHING = 0
        FISHING = 1
        SPLASHING = 2
    state = States.NOT_FISHING

    fishing_avg_x = 0
    fishing_total_x = 0
    fishing_avg_y = 0
    fishing_total_y = 0
    fishing_avg_frames = 0
    deformation = 0

    deformation_h_total = 0
    deformation_h_avg = 0
    deformation_h = 0

    anomaly_count = 0

    paused = False
    while(True):
        if not paused:
            screen = grab_screen(region=(region_x1,region_y1,region_x2,region_y2))
            screen_gaussian = cv2.GaussianBlur(screen, (5, 5), 0)
            
            last_time = time.time()
            
            screen_hsv = cv2.cvtColor(screen_gaussian, cv2.COLOR_BGR2HSV)
            screen = cv2.cvtColor(screen, cv2.COLOR_BGR2RGB)

            min_x = 1920
            min_y = 1080
            max_x = 0
            max_y = 0           

            # Detect bobber via gaussian + HSV colour filtering + biggest contour.
            lower_filter = np.array([0, 20, 100])
            upper_filter = np.array([80, 100, 200])
            mask = cv2.inRange(screen_hsv, lower_filter, upper_filter)

            # Bonus: dilation/erosion to get rid of the tiny trailing shit and amplify the main segment of the bobber.
            kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (8,8))
            mask_eroded = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)
            mask_dilated = cv2.morphologyEx(mask_eroded, cv2.MORPH_OPEN, kernel)

            _, contours, _ = cv2.findContours(mask_dilated, cv2.RETR_TREE, cv2.CHAIN_APPROX_NONE)
            try:
                biggest_contour = contours[0]
                for contour in contours:
                    if(cv2.contourArea(contour) > cv2.contourArea(biggest_contour)):
                        biggest_contour = contour
            except:
                if(state == States.FISHING):
                    state = States.SPLASHING
                    print("Nothing found, but in fishing state, assuming it's a splash.")
                    interval_delay = 0.2 + (random.randint(200, 1200) / 1000)
                    print("Sleeping %.2fs" % interval_delay)
                    time.sleep(interval_delay)
                else:
                    print("Nothing found (no contours), skipping for 1s.")
                    time.sleep(1)
                    continue;
            if(cv2.contourArea(biggest_contour) < 18):
                print("Nothing found (contour too small), skipping for 1s.")
                time.sleep(1)
                continue;
                
            #cv2.drawContours(screen, biggest_contour, -1, (0, 255, 0), 3)

            contour_points = np.int0(biggest_contour)
            for point in contour_points:
                x, y = point.ravel()
                if x < min_x:
                    min_x = x
                elif x > max_x:
                    max_x = x
                if y < min_y:
                    min_y = y
                elif y > max_y:
                    max_y = y

            cv2.rectangle(screen, (min_x, min_y), (max_x, max_y), (0,255,0), 2)

            size_x = max_x - min_x
            size_y = max_y - min_y

            # Detect splash via hue average around bobber.  May use square deviation too, not sure yet.
            min_y_splash = min_y - 35 
            max_y_splash = min_y_splash + size_y + 35
            min_x_splash = min_x - 30
            max_x_splash = min_x_splash + size_x + 40
            # There are two to preserve relative coordinates between cropping.
            min_y_splash2 = max_y_splash
            max_y_splash2 = 0
            min_x_splash2 = max_y_splash
            max_x_splash2 = 0

            screen_splash = screen_hsv[min_y_splash: max_y_splash, min_x_splash: max_x_splash]
            avg_h = 0
            total_h = 0
            val_count = 0
            for pixels in screen_splash:
               for hsv in pixels:
                   #h, _, _ = hsv.ravel()
                   #trialing luminosity instead:
                   _, _, h = hsv.ravel()
                   total_h += h
                   val_count += 1
            try:
                avg_h = total_h / val_count
            except:
                pass
            
            if(state == States.FISHING):
                fishing_avg_frames += 1
                fishing_total_x += size_x
                fishing_total_y += size_y
                fishing_avg_x = fishing_total_x / fishing_avg_frames
                fishing_avg_y = fishing_total_y / fishing_avg_frames
                deformation = abs(fishing_avg_x - size_x) + abs(fishing_avg_y - size_y)

                deformation_h_total += avg_h
                deformation_h_avg = deformation_h_total / fishing_avg_frames
                deformation_h = abs(deformation_h_avg - avg_h)

                cv2.rectangle(screen, (min_x_splash, min_y_splash), (max_x_splash, max_y_splash), (0,0,255), 2)

            elif((size_x < 62 and size_y < 62) and (size_x > 16 and size_y > 16)):
                state = States.FISHING
                print("Entering fishing state")
            elif(state != States.SPLASHING):
                state = States.NOT_FISHING
                fishing_avg_x = 0
                fishing_total_x = 0
                fishing_avg_y = 0
                fishing_total_y = 0
                fishing_avg_frames = 0
                deformation = 0
                deformation_h_total = 0
                deformation_h_avg = 0
                deformation_h = 0
                anomaly_count = 0
           
            #(hue) if(deformation > 5 and deformation_h > 1.4):
            if(deformation > 5 and deformation_h > 8):
                state = States.SPLASHING
                print("Entering splashing state")
                interval_delay = 0.2 + (random.randint(200, 1200) / 1000)
                print("Sleeping %.2fs" % interval_delay)
                time.sleep(interval_delay)

            # Debugging/viewing purposes.
            screen = cv2.resize(screen,None,fx=2, fy=2, interpolation = cv2.INTER_NEAREST)
            cv2.imshow('KrippitOS v3.0 With Gilly Gagging On Fresh Dog Sausage', screen)

            if(state == States.SPLASHING):
                move_to((region_x1 + ((min_x + max_x) / 2)), (region_y1 + ((min_y + max_y) / 2)), 1)
                interval_delay = 0.7 + (random.randint(300, 700) / 1000)
                print("Sleeping %.2fs" % interval_delay)
                time.sleep(interval_delay)

                move_to((random.randint(1, 3300)), (500 + random.randint(1, 500)), 0)
                interval_delay = 3
                print("Sleeping %.2fs" % interval_delay)
                time.sleep(interval_delay)

                fishing_avg_x = 0
                fishing_total_x = 0
                fishing_avg_y = 0
                fishing_total_y = 0
                fishing_avg_frames = 0
                deformation = 0
                deformation_h_total = 0
                deformation_h_avg = 0
                deformation_h = 0
                anomaly_count = 0

                state = States.NOT_FISHING

            if cv2.waitKey(25) & 0xFF == ord('q'):
                cv2.destroyAllWindows()
                break

main()
