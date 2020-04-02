import sys
import os
import asyncio
import functools
from concurrent.futures import CancelledError
import uuid
import logging
from hubManager import HubManager
from sensehat import Sense_Hat

logging.basicConfig(format='%(asctime)s - %(message)s', level=logging.INFO)

def stdin_listener():
    while True:
        selection = input("Press Q to quit\n")
        if selection == "Q" or selection == "q":
            print("Quitting...")
            break

async def main():

    connectionString = os.getenv("IOTHUB_DEVICE_CONNECTION_STRING")

    try:
        if connectionString == None:
            logging.error('Please set IOTHUB_DEVICE_CONNECTION_STRING environment variable')
            return
        else:
            logging.info('Connection String : {}'.format(connectionString))

        async with HubManager(connectionString) as hubManager:

            # Enable SenseHat

            sense = Sense_Hat(hubManager)

            listeners = asyncio.gather(
                asyncio.create_task(hubManager.twin_patch_listener()),
                asyncio.create_task(hubManager.message_listener()),
                asyncio.create_task(hubManager.generic_method_listener()),
                return_exceptions=True
            )

            loop = asyncio.get_running_loop()

            senseHatTask = asyncio.create_task(sense.telemetry_worker())

            # keyboard input
            inputTask = loop.run_in_executor(None, stdin_listener)

            await inputTask

            # # cancel tasks
            senseHatTask.cancel()
            listeners.cancel()

            # # wait for tasks to complete
            await senseHatTask
            await listeners

    except CancelledError:
        logging.info('-- {0}() - Cancelled'.format(sys._getframe().f_code.co_name))

    except Exception as ex:
        logging.info('<< {0}() ****  Exception {1} ****'.format(sys._getframe().f_code.co_name, ex))

    # finally:
    #     logging.info('<< {0}()'.format(sys._getframe().f_code.co_name))

if __name__ == "__main__":

    #
    # get_running_loop was added to v3.7
    #
    if (sys.version_info >= (3, 7)):
        asyncio.run(main())
    else:
        loop = asyncio.get_event_loop()
        loop.run_until_complete(main())
        loop.close()