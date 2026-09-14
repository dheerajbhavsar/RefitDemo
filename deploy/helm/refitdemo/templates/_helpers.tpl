{{/*
Expand the name of the chart.
*/}}
{{- define "refitdemo.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "refitdemo.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "refitdemo.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "refitdemo.labels" -}}
helm.sh/chart: {{ include "refitdemo.chart" . }}
{{ include "refitdemo.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: refitdemo-platform
{{- end }}

{{/*
Selector labels
*/}}
{{- define "refitdemo.selectorLabels" -}}
app.kubernetes.io/name: {{ include "refitdemo.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "refitdemo.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "refitdemo.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Determine the secret name for TMDB credentials
*/}}
{{- define "refitdemo.secretName" -}}
{{- if .Values.tmdb.existingSecret }}
{{- .Values.tmdb.existingSecret }}
{{- else }}
{{- printf "%s-secret" (include "refitdemo.fullname" .) }}
{{- end }}
{{- end }}
