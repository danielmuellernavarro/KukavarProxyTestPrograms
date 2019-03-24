import sys
from PySide2 import QtGui, QtCore, QtWidgets
from ui import Ui_MainWindow
import time
import os
from py_openshowvar import openshowvar

roboterIp = '192.168.178.24'
roboterPort = 7000
debugKVP = False

__project__ = 'Test Kuka TCP-IP'
__version__ = '0.1'

class ClientTest(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
        self.roboterIp = roboterIp
        self.roboterPort = roboterPort
        self.debugKVP = debugKVP
        self.windowsTitle = __project__ + " " + __version__

        self.Ui_MainWindow = Ui_MainWindow.Ui_MainWindow()
        self.Ui_MainWindow.setupUi(self)
        self.setWindowTitle(self.windowsTitle)

        self.Ui_MainWindow.InputIp.setText(roboterIp)
        self.Ui_MainWindow.ButtonIp.clicked.connect(self.button_ip)
        self.Ui_MainWindow.ButtonRead.clicked.connect(self.button_read)
        self.Ui_MainWindow.ButtonWrite.clicked.connect(self.button_write)

    def button_ip(self):
        self.client = openshowvar(self.Ui_MainWindow.InputIp.text(), self.roboterPort)
        if self.client.can_connect:
            self.Ui_MainWindow.TextStatus.setText('Connected to: {0}'.format(self.client.read('$ROBNAME[]', self.debugKVP)))
        else:
            self.Ui_MainWindow.TextStatus.setText('The conection with the robot was not posible')

    def button_read(self):
        InputRead =  self.Ui_MainWindow.InputRead.text()
        try:
            self.Ui_MainWindow.TextReadResult.setText('{0}'.format(self.client.read(InputRead, self.debugKVP)))
        except:
            self.Ui_MainWindow.TextReadResult.setText('The variable was not read')
            self.ButtonIp()

    def button_write(self):
        InputWrite = self.Ui_MainWindow.InputWrite.text()
        InputValue = self.Ui_MainWindow.InputValue.text()
        try:
            self.Ui_MainWindow.TextWriteResult.setText('{0}'.format(self.client.write(InputWrite, InputValue, self.debugKVP)))
        except:
            self.Ui_MainWindow.TextWriteResult.setText('The variable was not written')
            self.ButtonIp()

def main():
    qt_app = QtWidgets.QApplication(sys.argv)
    q = ClientTest()
    q.show()

    sys.exit(qt_app.exec_())

if __name__ == '__main__':
    main()
