# pip install pyqt5-tools
import os

def convert():
    PYSIDE_UIC = 'C:\Python36-64\Scripts\pyside2-uic.exe'

    ui_files = []
    ui_files += [each for each in os.listdir(os.getcwd()) if each.endswith('.ui')]

    for filename in ui_files:
        py_filename = 'Ui_' + os.path.splitext(filename)[-2] + '.py'

        os_string = PYSIDE_UIC + ' ' + filename + ' -o ' + py_filename
        print(os_string)
        if os.system(os_string) != 0:
            print('File ' + filename + ' could not be generated')
        else: print('File ' + filename + ' created successfully')

if __name__ == '__main__':
    convert()
