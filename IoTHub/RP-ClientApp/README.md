# Raspberry Pi IoT Device App

## Requirements

- Raspberry Pi 3 or 4
- SenseHat

## Setup

```bash
#!/bin/bash

newHostName=rp3
sudo echo 'pi:Password!1' | sudo chpasswd

sudo apt-get update && \
sudo apt-get upgrade -y && \
sudo apt-get install -y python3-pip sense-hat

cd ~/ && \
sudo raspi-config nonint do_expand_rootfs
sudo raspi-config nonint do_memory_split 16
sudo raspi-config nonint do_spi 0
sudo raspi-config nonint do_i2c 0
sudo raspi-config nonint do_wifi_country US
sudo raspi-config nonint do_change_locale en_US.UTF-8
sudo raspi-config nonint do_configure_keyboard us
sudo raspi-config nonint do_hostname $newHostName
sudo reboot now

```

