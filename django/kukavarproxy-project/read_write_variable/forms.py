from django import forms
from datetime import datetime


class StateForm(forms.Form):
    prefix = 'form_state'
    IP = forms.GenericIPAddressField(
        initial = '192.168.3.148',
        max_length = 30,
        widget = forms.TextInput(
            attrs = {
                # 'style': 'border-color: blue;',
                # 'placeholder': 'Write your name here'
            }
        ),
    )
    state = forms.CharField(
        initial = '',
        required = False,
        max_length = 30,
        disabled = True,
        widget = forms.TextInput(
            attrs = {
                # 'style': 'border-color: blue;',
                # 'placeholder': 'Write your name here'
            }
        )
    )

class ReadForm(forms.Form):
    prefix = 'form_read'
    variable = forms.CharField(
        initial = '$OV_PRO',
        max_length = 255,
    )
    result = forms.CharField(
        initial = '',
        required = False,
        max_length = 255,
        disabled = True,
    )

class WriteForm(forms.Form):
    prefix = 'form_write'
    variable = forms.CharField(
        initial = '$OV_PRO',
        max_length = 255,
    )
    value = forms.CharField(
        initial = '',
        max_length = 255,
    )
    result = forms.CharField(
        initial = '',
        required = False,
        max_length = 255,
        disabled = True,
    )
