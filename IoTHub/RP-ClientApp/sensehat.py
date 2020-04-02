import sys
import datetime
import asyncio
from functools import partial
from concurrent.futures import CancelledError
import logging
from sense_hat import SenseHat,ACTION_PRESSED, ACTION_HELD, ACTION_RELEASED

# https://pythonhosted.org/sense-hat/api/
# https://github.com/astro-pi/python-sense-hat/blob/master/docs/api.md

class Sense_Hat:

    def __init__(self,
                 hubManager):

        logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))
        self.verbose = True
        self.hubManager = hubManager
        self.senseHat = SenseHat()
        self.messageId = 0
        # Compass, Gyro, Accelerometer
        # self.senseHat.set_imu_config(True, False, False)

        self.senseHat.stick.direction_up = self.joystick_event
        self.senseHat.stick.direction_down = self.joystick_event
        self.senseHat.stick.direction_right = self.joystick_event
        self.senseHat.stick.direction_left = self.joystick_event
        self.senseHat.stick.direction_down = self.joystick_event
        self.senseHat.clear()
        self.displayFeature = None

        self.telemetryIntervalinMs = 60000

    async def __aenter__(self):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        if self.hubManager.deviceClient.connected:
            self.display_Message("Connected")

        #self.display_Message("Connected")

    async def __aexit__(self, exception_type, exception_value, traceback):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        if self.senseHat:
            self.senseHat.clear()

        if self.displayFeature:
            await self.displayFeature

    async def telemetry_worker(self):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        if self.hubManager.deviceClient.connected:
            self.display_Message("Connected")

        try:

            while True:
                # temperature = self.senseHat.get_temperature()
                # humidity = self.senseHat.get_humidity()
                # use the same telemetry with https://azure-samples.github.io/raspberry-pi-web-simulator/
                self.messageId += 1
                message = {
                    "timestamp" : datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%f')[:-3] + 'Z',
                    "messageId" : self.messageId,
                    "deviceId" : self.hubManager.deviceId,
                    "temperature": '{:.3f}'.format(self.senseHat.temperature),
                    "humidity":'{:.3f}'.format(self.senseHat.humidity),
#                    "pressure":'{:.3f}'.format(self.senseHat.pressure),
                }

                await self.hubManager.send_Message(message)

                await asyncio.sleep(self.telemetryIntervalinMs/1000)

        except CancelledError:
            logging.info('-- {0}:{1}() - Cancelled'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        except Exception as ex:
            logging.info('<< {0}:{1}() ****  Exception {2} ****'.format(self.__class__.__name__, sys._getframe().f_code.co_name, ex))

        finally:
            if self.verbose:
                logging.info('<< {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

    def task_done(self, feature):
        self.displayFeature = None

    def display_Message(self, message, color=[255,255,255]):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        loop = asyncio.get_running_loop()
        self.displayFeature = loop.run_in_executor(None, partial(self.senseHat.show_message, text_string=message, text_colour=color))
        self.displayFeature.add_done_callback(self.task_done)

    def joystick_listner(self):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        try:
            while True:
                # Wait for Desired Twin Update
                event = self.senseHat.stick.wait_for_event()

                if event.action != ACTION_RELEASED:
                    message = {
                        "timestamp": datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%f')[:-3] + 'Z',
                        "joystick_event": '{}'.format(event.direction),
                        "timestamp":'{}'.format(event.timestamp),
                    }

                    asyncio.run(self.hubManager.send_Message(message = message, messageType="Event"))

                logging.info('Joystick Direction : {} {}'.format(event.action, event.direction))

        except CancelledError:
            logging.info('-- {0}:{1}() - Cancelled'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        except Exception as ex:
            logging.info('<< {0}:{1}() ****  Exception {2} ****'.format(self.__class__.__name__, sys._getframe().f_code.co_name, ex))

        finally:
            if self.verbose:
                logging.info('<< {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        return 0

    def joystick_event(self, event):

        if self.verbose:
            logging.info('>> {0}:{1}()'.format(self.__class__.__name__, sys._getframe().f_code.co_name))

        try:
            if event.action != ACTION_RELEASED:

                message = {
                    "timestamp": datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%f')[:-3] + 'Z',
                    "joystick_event": '{}'.format(event.direction)
                }

                asyncio.run(self.hubManager.send_Message(message = message, messageType="Event"))

        except Exception as ex:
            logging.info('<< {0}:{1}() ****  Exception {2} ****'.format(self.__class__.__name__, sys._getframe().f_code.co_name, ex))

