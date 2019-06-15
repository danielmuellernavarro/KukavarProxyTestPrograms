from django.db import models


class Project(models.Model):
    name = models.CharField(max_length=32)
    description = models.CharField(max_length=256, null=True, blank=True)
    IP = models.GenericIPAddressField()

    def __str__(self):
        return self.name

    class Meta:
        ordering = ('name',)


class Variable(models.Model):
    project = models.ForeignKey(Project, on_delete=models.CASCADE)
    name = models.CharField(max_length=256)
    description = models.CharField(max_length=64, null=True, blank=True)

    def __str__(self):
        return self.project.name + ' -- ' + self._description

    @property
    def _description(self):
        if self.description:
            return self.description
        else:
            return self.name
    # def _description(self)
    #     if self.description:
    #         return self.description
    #     else:
    #         return self.name
    #
    def as_json(self):
        return dict(
            description=self._description,
            name=self.name
            )

    class Meta:
        ordering = ('description',)
