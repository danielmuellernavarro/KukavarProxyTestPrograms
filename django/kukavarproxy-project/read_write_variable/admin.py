from django.contrib import admin
from .models import Project, Variable

class VariableAdmin(admin.ModelAdmin):

    list_display = ('__str__', 'project', 'name', 'description',)
    list_editable = ( 'name', 'description', )

    search_fields = ['name', 'description', 'project__name',]
    ordering = ('project__name', 'name',)

admin.site.register(Variable, VariableAdmin)

admin.site.register(Project)
