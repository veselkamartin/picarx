cd ~/picar-dotnet/
git pull

sudo -E dotnet run

sudo raspi-config

git clone -b v2.0 https://github.com/sunfounder/picar-x.git
git clone -b picamera2 https://github.com/sunfounder/vilib.git
git clone -b v2.0 https://github.com/sunfounder/robot-hat.git
sudo apt upgrade

sudo apt install git python3-pip python3-setuptools python3-smbus
sudo apt-get install mpg123
sudo apt-get install libgdiplus libx11-dev libgeotiff-dev  libxt-dev libopengl-dev libglx-dev libusb-1.0-0
sudo apt-get install python3-vtk9
sudo apt-get install libopenal-dev
sudo bash i2samp.sh

cd ~/picar-x/example/
sudo python3 13.app_control.py

ldd libcvextern.so | grep "not found"

raspistill -o image.jpg
sudo i2cdetect -y 1
sudo espeak hello

nano ~/.bashrc
export OPENAI_API_KEY=...
export AZURE_SPEACH_KEY=...
