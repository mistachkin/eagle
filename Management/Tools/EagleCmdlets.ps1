###############################################################################
#
# EagleCmdlets.ps1 --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# PowerShell Cmdlet Installation Script
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

param([String]$mode)

$version = "v" + [System.Environment]::Version.ToString(3)

if ([IntPtr]::Size -eq 8) {
  Set-Alias InstallUtil `
      "$env:windir\Microsoft.NET\Framework64\$version\InstallUtil.exe"
} else {
  Set-Alias InstallUtil `
      "$env:windir\Microsoft.NET\Framework\$version\InstallUtil.exe"
}

$path = Split-Path $myInvocation.myCommand.path

if ($path -eq $null) {
  $path = "."
}

$fileName = Join-Path $path EagleCmdlets.dll

switch ($mode)
{
  "install" {
    InstallUtil /LogFile= $fileName

    if ($lastExitCode -eq 0) {
      Add-PSSnapin EagleCmdlets
    } else {
      Write-Error "Installation failed."
    }
  }
  "uninstall" {
    InstallUtil /u /LogFile= $fileName

    if ($lastExitCode -ne 0) {
      Write-Error "Uninstallation failed."
    }
  }
  default {
    Write-Error "Unknown mode."
  }
}

# SIG # Begin signature block
# MIIiwwYJKoZIhvcNAQcCoIIitDCCIrACAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCytsH/3wnHw07u
# VDsPuJztC58MUZpF3Tm6Dmq8S7bXpaCCHdMwggPFMIICraADAgECAhACrFwmagtA
# m48LefKuRiV3MA0GCSqGSIb3DQEBBQUAMGwxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xKzApBgNV
# BAMTIkRpZ2lDZXJ0IEhpZ2ggQXNzdXJhbmNlIEVWIFJvb3QgQ0EwHhcNMDYxMTEw
# MDAwMDAwWhcNMzExMTEwMDAwMDAwWjBsMQswCQYDVQQGEwJVUzEVMBMGA1UEChMM
# RGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSswKQYDVQQD
# EyJEaWdpQ2VydCBIaWdoIEFzc3VyYW5jZSBFViBSb290IENBMIIBIjANBgkqhkiG
# 9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxszlc+b71LvlLS0ypt/lgT/JzSVJtnEqw9WU
# NGeiChywX2mmQLHEt7KP0JikqUFZOtPclNY823Q4pErMTSWC90qlUxI47vNJbXGR
# fmO2q6Zfw6SE+E9iUb74xezbOJLjBuUIkQzEKEFV+8taiRV+ceg1v01yCT2+OjhQ
# W3cxG42zxyRFmqesbQAUWgS3uhPrUQqYQUEiTmVhh4FBUKZ5XIneGUpX1S7mXRxT
# LH6YzRoGFqRoc9A0BBNcoXHTWnxV215k4TeHMFYE5RG0KYAS8Xk5iKICEXwnZreI
# t3jyygqoOKsKZMK/Zl2VhMGhJR6HXRpQCyASzEG7bgtROLhLywIDAQABo2MwYTAO
# BgNVHQ8BAf8EBAMCAYYwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUsT7DaQP4
# v0cB1JgmGggC72NkK8MwHwYDVR0jBBgwFoAUsT7DaQP4v0cB1JgmGggC72NkK8Mw
# DQYJKoZIhvcNAQEFBQADggEBABwaBpfc15yfPIhmBghXIdshR/gqZ6q/GDJ2QBBX
# wYrzetkRZY41+p78RbWe2UwxS7iR6EMsjrN4ztvjU3lx1uUhlAHaVYeaJGT2imbM
# 3pw3zag0sWmbI8ieeCIrcEPjVUcxYRnvWMWFL04w9qAxFiPI5+JlFjPLvxoboD34
# yl6LMYtgCIktDAZcUrfE+QqY0RVfnxK+fDZjOL1EpH/kJisKxJdpDemM4sAQV7jI
# dhKRVfJIadi8KgJbD0TUIDHb9LpwJl2QYJ68SxcJL7TLHkNoyQcnwdJc9+ohuWgS
# nDycv578gFybY83sR6olJ2egN/MAgn1U16n46S4To3foH0owggYHMIIE76ADAgEC
# AhAEX9mjEMlaqTBoBO6h4DF6MA0GCSqGSIb3DQEBDQUAMGwxCzAJBgNVBAYTAlVT
# MRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5j
# b20xKzApBgNVBAMTIkRpZ2lDZXJ0IEVWIENvZGUgU2lnbmluZyBDQSAoU0hBMikw
# HhcNMTUxMTE3MDAwMDAwWhcNMTgxMTIxMTIwMDAwWjCCARsxGDAWBgNVBA8MD0J1
# c2luZXNzIEVudGl0eTETMBEGCysGAQQBgjc8AgEDEwJVUzEXMBUGCysGAQQBgjc8
# AgECEwZPcmVnb24xEzARBgNVBAUTCjAxLzEyLzIwMTUxHjAcBgNVBAkTFTIyMjkg
# TlcgU0hFRkZJRUxEIEFWRTEOMAwGA1UEERMFOTcwMDYxCzAJBgNVBAYTAlVTMQ8w
# DQYDVQQIEwZPcmVnb24xEjAQBgNVBAcTCUJlYXZlcnRvbjEsMCoGA1UEChMjTWlz
# dGFjaGtpbiBTeXN0ZW1zIChKb2UgTWlzdGFjaGtpbikxLDAqBgNVBAMTI01pc3Rh
# Y2hraW4gU3lzdGVtcyAoSm9lIE1pc3RhY2hraW4pMIIBIjANBgkqhkiG9w0BAQEF
# AAOCAQ8AMIIBCgKCAQEA0wk0gSU7vem9ZlIqQZDkjzXH5UqaeqWHz3EaCFF2MOyX
# udSsXf06OIm4JBILMY7rtZ0DWvFB7Nrb+IjxsDoulWZqX3i9UbFqgReyINKDhydg
# HBmWsjjYhxv7SpY0CR/aZs2TEFQkhyJPim3N2GIn4u1RAtSCXWt/oFTZ4uDu2UX2
# UM2IK0blKmxGtopgZeobUCnCA4uESqCR/g7MZpbzilAJaf/tKfMwdCEnTtGn1eZw
# S1H0R7GGNWU+3Rdsmkn1oQQp863sugYFHgCZbm/OZUBJUZv0R7A7N+ZPXog8LDMB
# bIWdhUmQRgBpjkK+ykoL2lkJhpU7ivf0Lt70TpO5wwIDAQABo4IB8jCCAe4wHwYD
# VR0jBBgwFoAUj+h+8G0yagAFI8dwl2o6kP9r6tQwHQYDVR0OBBYEFPr3ODKD87cq
# /xAIoUC9oKXTNGKiMC8GA1UdEQQoMCagJAYIKwYBBQUHCAOgGDAWDBRVUy1PUkVH
# T04tMDEvMTIvMjAxNTAOBgNVHQ8BAf8EBAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUH
# AwMwewYDVR0fBHQwcjA3oDWgM4YxaHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0VW
# Q29kZVNpZ25pbmdTSEEyLWcxLmNybDA3oDWgM4YxaHR0cDovL2NybDQuZGlnaWNl
# cnQuY29tL0VWQ29kZVNpZ25pbmdTSEEyLWcxLmNybDBLBgNVHSAERDBCMDcGCWCG
# SAGG/WwDAjAqMCgGCCsGAQUFBwIBFhxodHRwczovL3d3dy5kaWdpY2VydC5jb20v
# Q1BTMAcGBWeBDAEDMH4GCCsGAQUFBwEBBHIwcDAkBggrBgEFBQcwAYYYaHR0cDov
# L29jc3AuZGlnaWNlcnQuY29tMEgGCCsGAQUFBzAChjxodHRwOi8vY2FjZXJ0cy5k
# aWdpY2VydC5jb20vRGlnaUNlcnRFVkNvZGVTaWduaW5nQ0EtU0hBMi5jcnQwDAYD
# VR0TAQH/BAIwADANBgkqhkiG9w0BAQ0FAAOCAQEAA8nRcIx05V8py/IYKxXi1HUI
# 7vsf0lNf8FEdPviFCzRVjIZ1p7w3+xGVUFkI2EsXs50C0xDYCZWuEaaC47ilo91V
# /QJeOK33EPs38BEASnVs91hFsSuJG9mJq1z8vFbbZH8HjnQkRJBz4FDo3VSf03vp
# 7nu6aSJJllmchoxP/Hw+hL874d0KoJW/DR1FMHVXMb0aq8vXUWSRh/mc+cFF7Y4c
# gCwXY5hlQiVsrwYHPRxACXr961TJCkYbMy9lmUtQ8KgtaCIkZlaXOhqLlVu4HQep
# GUZ5y6VkV4XAFON4bnA2HSQwBoSCQJoVxlT6tlg8Iwa35ee4guzTLTleKo+44TCC
# BmowggVSoAMCAQICEAMBmgI6/1ixa9bV6uYX8GYwDQYJKoZIhvcNAQEFBQAwYjEL
# MAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZMBcGA1UECxMQd3d3
# LmRpZ2ljZXJ0LmNvbTEhMB8GA1UEAxMYRGlnaUNlcnQgQXNzdXJlZCBJRCBDQS0x
# MB4XDTE0MTAyMjAwMDAwMFoXDTI0MTAyMjAwMDAwMFowRzELMAkGA1UEBhMCVVMx
# ETAPBgNVBAoTCERpZ2lDZXJ0MSUwIwYDVQQDExxEaWdpQ2VydCBUaW1lc3RhbXAg
# UmVzcG9uZGVyMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAo2Rd/Hyz
# 4II14OD2xirmSXU7zG7gU6mfH2RZ5nxrf2uMnVX4kuOe1VpjWwJJUNmDzm9m7t3L
# helfpfnUh3SIRDsZyeX1kZ/GFDmsJOqoSyyRicxeKPRktlC39RKzc5YKZ6O+YZ+u
# 8/0SeHUOplsU/UUjjoZEVX0YhgWMVYd5SEb3yg6Np95OX+Koti1ZAmGIYXIYaLm4
# fO7m5zQvMXeBMB+7NgGN7yfj95rwTDFkjePr+hmHqH7P7IwMNlt6wXq4eMfJBi5G
# EMiN6ARg27xzdPpO2P6qQPGyznBGg+naQKFZOtkVCVeZVjCT88lhzNAIzGvsYkKR
# rALA76TwiRGPdwIDAQABo4IDNTCCAzEwDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB
# /wQCMAAwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwgwggG/BgNVHSAEggG2MIIBsjCC
# AaEGCWCGSAGG/WwHATCCAZIwKAYIKwYBBQUHAgEWHGh0dHBzOi8vd3d3LmRpZ2lj
# ZXJ0LmNvbS9DUFMwggFkBggrBgEFBQcCAjCCAVYeggFSAEEAbgB5ACAAdQBzAGUA
# IABvAGYAIAB0AGgAaQBzACAAQwBlAHIAdABpAGYAaQBjAGEAdABlACAAYwBvAG4A
# cwB0AGkAdAB1AHQAZQBzACAAYQBjAGMAZQBwAHQAYQBuAGMAZQAgAG8AZgAgAHQA
# aABlACAARABpAGcAaQBDAGUAcgB0ACAAQwBQAC8AQwBQAFMAIABhAG4AZAAgAHQA
# aABlACAAUgBlAGwAeQBpAG4AZwAgAFAAYQByAHQAeQAgAEEAZwByAGUAZQBtAGUA
# bgB0ACAAdwBoAGkAYwBoACAAbABpAG0AaQB0ACAAbABpAGEAYgBpAGwAaQB0AHkA
# IABhAG4AZAAgAGEAcgBlACAAaQBuAGMAbwByAHAAbwByAGEAdABlAGQAIABoAGUA
# cgBlAGkAbgAgAGIAeQAgAHIAZQBmAGUAcgBlAG4AYwBlAC4wCwYJYIZIAYb9bAMV
# MB8GA1UdIwQYMBaAFBUAEisTmLKZB+0e36K+Vw0rZwLNMB0GA1UdDgQWBBRhWk0k
# tkkynUoqeRqDS/QeicHKfTB9BgNVHR8EdjB0MDigNqA0hjJodHRwOi8vY3JsMy5k
# aWdpY2VydC5jb20vRGlnaUNlcnRBc3N1cmVkSURDQS0xLmNybDA4oDagNIYyaHR0
# cDovL2NybDQuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEQ0EtMS5jcmww
# dwYIKwYBBQUHAQEEazBpMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2Vy
# dC5jb20wQQYIKwYBBQUHMAKGNWh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9E
# aWdpQ2VydEFzc3VyZWRJRENBLTEuY3J0MA0GCSqGSIb3DQEBBQUAA4IBAQCdJX4b
# M02yJoFcm4bOIyAPgIfliP//sdRqLDHtOhcZcRfNqRu8WhY5AJ3jbITkWkD73gYB
# jDf6m7GdJH7+IKRXrVu3mrBgJuppVyFdNC8fcbCDlBkFazWQEKB7l8f2P+fiEUGm
# vWLZ8Cc9OB0obzpSCfDscGLTYkuw4HOmksDTjjHYL+NtFxMG7uQDthSr849Dp3Gd
# Id0UyhVdkkHa+Q+B0Zl0DSbEDn8btfWg8cZ3BigV6diT5VUW8LsKqxzbXEgnZsij
# iwoc5ZXarsQuWaBh3drzbaJh6YoLbewSGL33VVRAA5Ira8JRwgpIr7DUbuD0FAo6
# G+OPPcqvao173NhEMIIGvDCCBaSgAwIBAgIQA/G04V86gvEUlniz19hHXDANBgkq
# hkiG9w0BAQsFADBsMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5j
# MRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSswKQYDVQQDEyJEaWdpQ2VydCBI
# aWdoIEFzc3VyYW5jZSBFViBSb290IENBMB4XDTEyMDQxODEyMDAwMFoXDTI3MDQx
# ODEyMDAwMFowbDELMAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZ
# MBcGA1UECxMQd3d3LmRpZ2ljZXJ0LmNvbTErMCkGA1UEAxMiRGlnaUNlcnQgRVYg
# Q29kZSBTaWduaW5nIENBIChTSEEyKTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCC
# AQoCggEBAKdT+g+ytRPxZM+EgPyugDXRttfHoyysGiys8YSsOjUSOpKRulfkxMnz
# L6hIPLfWbtyXIrpReWGvQy8Nt5u0STGuRFg+pKGWp4dPI37DbGUkkFU+ocojfMVC
# 6cR6YkWbfd5jdMueYyX4hJqarUVPrn0fyBPLdZvJ4eGK+AsMmPTKPtBFqnoepViT
# NjS+Ky4rMVhmtDIQn53wUqHv6D7TdvJAWtz6aj0bS612sIxc7ja6g+owqEze8Qsq
# WEGIrgCJqwPRFoIgInbrXlQ4EmLh0nAk2+0fcNJkCYAt4radzh/yuyHzbNvYsxl7
# ilCf7+w2Clyat0rTCKA5ef3dvz06CSUCAwEAAaOCA1gwggNUMBIGA1UdEwEB/wQI
# MAYBAf8CAQAwDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoGCCsGAQUFBwMDMH8G
# CCsGAQUFBwEBBHMwcTAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGlnaWNlcnQu
# Y29tMEkGCCsGAQUFBzAChj1odHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5jb20vRGln
# aUNlcnRIaWdoQXNzdXJhbmNlRVZSb290Q0EuY3J0MIGPBgNVHR8EgYcwgYQwQKA+
# oDyGOmh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEhpZ2hBc3N1cmFu
# Y2VFVlJvb3RDQS5jcmwwQKA+oDyGOmh0dHA6Ly9jcmw0LmRpZ2ljZXJ0LmNvbS9E
# aWdpQ2VydEhpZ2hBc3N1cmFuY2VFVlJvb3RDQS5jcmwwggHEBgNVHSAEggG7MIIB
# tzCCAbMGCWCGSAGG/WwDAjCCAaQwOgYIKwYBBQUHAgEWLmh0dHA6Ly93d3cuZGln
# aWNlcnQuY29tL3NzbC1jcHMtcmVwb3NpdG9yeS5odG0wggFkBggrBgEFBQcCAjCC
# AVYeggFSAEEAbgB5ACAAdQBzAGUAIABvAGYAIAB0AGgAaQBzACAAQwBlAHIAdABp
# AGYAaQBjAGEAdABlACAAYwBvAG4AcwB0AGkAdAB1AHQAZQBzACAAYQBjAGMAZQBw
# AHQAYQBuAGMAZQAgAG8AZgAgAHQAaABlACAARABpAGcAaQBDAGUAcgB0ACAAQwBQ
# AC8AQwBQAFMAIABhAG4AZAAgAHQAaABlACAAUgBlAGwAeQBpAG4AZwAgAFAAYQBy
# AHQAeQAgAEEAZwByAGUAZQBtAGUAbgB0ACAAdwBoAGkAYwBoACAAbABpAG0AaQB0
# ACAAbABpAGEAYgBpAGwAaQB0AHkAIABhAG4AZAAgAGEAcgBlACAAaQBuAGMAbwBy
# AHAAbwByAGEAdABlAGQAIABoAGUAcgBlAGkAbgAgAGIAeQAgAHIAZQBmAGUAcgBl
# AG4AYwBlAC4wHQYDVR0OBBYEFI/ofvBtMmoABSPHcJdqOpD/a+rUMB8GA1UdIwQY
# MBaAFLE+w2kD+L9HAdSYJhoIAu9jZCvDMA0GCSqGSIb3DQEBCwUAA4IBAQAZM0oM
# gTM32602yeTJOru1Gy56ouL0Q0IXnr9OoU3hsdvpgd2fAfLkiNXp/gn9IcHsXYDS
# 8NbBQ8L+dyvb+deRM85s1bIZO+Yu1smTT4hAjs3h9X7xD8ZZVnLo62pBvRzVRtV8
# ScpmOBXBv+CRcHeH3MmNMckMKaIz7Y3ih82JjT8b/9XgGpeLfNpt+6jGsjpma3sB
# s83YpjTsEgGrlVilxFNXqGDm5wISoLkjZKJNu3yBJWQhvs/uQhhDl7ulNwavTf8m
# pU1hS+xGQbhlzrh5ngiWC4GMijuPx5mMoypumG1eYcaWt4q5YS2TuOsOBEPX9f6m
# 8GLUmWqlwcHwZJSAMIIGzTCCBbWgAwIBAgIQBv35A5YDreoACus/J7u6GzANBgkq
# hkiG9w0BAQUFADBlMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5j
# MRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSQwIgYDVQQDExtEaWdpQ2VydCBB
# c3N1cmVkIElEIFJvb3QgQ0EwHhcNMDYxMTEwMDAwMDAwWhcNMjExMTEwMDAwMDAw
# WjBiMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQL
# ExB3d3cuZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBBc3N1cmVkIElE
# IENBLTEwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDogi2Z+crCQpWl
# gHNAcNKeVlRcqcTSQQaPyTP8TUWRXIGf7Syc+BZZ3561JBXCmLm0d0ncicQK2q/L
# XmvtrbBxMevPOkAMRk2T7It6NggDqww0/hhJgv7HxzFIgHweog+SDlDJxofrNj/Y
# MMP/pvf7os1vcyP+rFYFkPAyIRaJxnCI+QWXfaPHQ90C6Ds97bFBo+0/vtuVSMTu
# HrPyvAwrmdDGXRJCgeGDboJzPyZLFJCuWWYKxI2+0s4Grq2Eb0iEm09AufFM8q+Y
# +/bOQF1c9qjxL6/siSLyaxhlscFzrdfx2M8eCnRcQrhofrfVdwonVnwPYqQ/MhRg
# lf0HBKIJAgMBAAGjggN6MIIDdjAOBgNVHQ8BAf8EBAMCAYYwOwYDVR0lBDQwMgYI
# KwYBBQUHAwEGCCsGAQUFBwMCBggrBgEFBQcDAwYIKwYBBQUHAwQGCCsGAQUFBwMI
# MIIB0gYDVR0gBIIByTCCAcUwggG0BgpghkgBhv1sAAEEMIIBpDA6BggrBgEFBQcC
# ARYuaHR0cDovL3d3dy5kaWdpY2VydC5jb20vc3NsLWNwcy1yZXBvc2l0b3J5Lmh0
# bTCCAWQGCCsGAQUFBwICMIIBVh6CAVIAQQBuAHkAIAB1AHMAZQAgAG8AZgAgAHQA
# aABpAHMAIABDAGUAcgB0AGkAZgBpAGMAYQB0AGUAIABjAG8AbgBzAHQAaQB0AHUA
# dABlAHMAIABhAGMAYwBlAHAAdABhAG4AYwBlACAAbwBmACAAdABoAGUAIABEAGkA
# ZwBpAEMAZQByAHQAIABDAFAALwBDAFAAUwAgAGEAbgBkACAAdABoAGUAIABSAGUA
# bAB5AGkAbgBnACAAUABhAHIAdAB5ACAAQQBnAHIAZQBlAG0AZQBuAHQAIAB3AGgA
# aQBjAGgAIABsAGkAbQBpAHQAIABsAGkAYQBiAGkAbABpAHQAeQAgAGEAbgBkACAA
# YQByAGUAIABpAG4AYwBvAHIAcABvAHIAYQB0AGUAZAAgAGgAZQByAGUAaQBuACAA
# YgB5ACAAcgBlAGYAZQByAGUAbgBjAGUALjALBglghkgBhv1sAxUwEgYDVR0TAQH/
# BAgwBgEB/wIBADB5BggrBgEFBQcBAQRtMGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9v
# Y3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NhY2VydHMuZGln
# aWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNydDCBgQYDVR0fBHow
# eDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJl
# ZElEUm9vdENBLmNybDA6oDigNoY0aHR0cDovL2NybDQuZGlnaWNlcnQuY29tL0Rp
# Z2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDAdBgNVHQ4EFgQUFQASKxOYspkH7R7f
# or5XDStnAs0wHwYDVR0jBBgwFoAUReuir/SSy4IxLVGLp6chnfNtyA8wDQYJKoZI
# hvcNAQEFBQADggEBAEZQPsm3KCSnOB22WymvUs9S6TFHq1Zce9UNC0Gz7+x1H3Q4
# 8rJcYaKclcNQ5IK5I9G6OoZyrTh4rHVdFxc0ckeFlFbR67s2hHfMJKXzBBlVqefj
# 56tizfuLLZDCwNK1lL1eT7EF0g49GqkUW6aGMWKoqDPkmzmnxPXOHXh2lCVz5Cqr
# z5x2S+1fwksW5EtwTACJHvzFebxMElf+X+EevAJdqP77BzhPDcZdkbkPZ0XN1oPt
# 55INjbFpjE/7WeAjD9KqrgB87pxCDs+R1ye3Fu4Pw718CqDuLAhVhSK46xgaTfwq
# Ia1JMYNHlXdx3LEbS0scEJx3FMGdTy9alQgpECYxggRGMIIEQgIBATCBgDBsMQsw
# CQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cu
# ZGlnaWNlcnQuY29tMSswKQYDVQQDEyJEaWdpQ2VydCBFViBDb2RlIFNpZ25pbmcg
# Q0EgKFNIQTIpAhAEX9mjEMlaqTBoBO6h4DF6MA0GCWCGSAFlAwQCAQUAoIGEMBgG
# CisGAQQBgjcCAQwxCjAIoAKAAKECgAAwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcC
# AQQwHAYKKwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIE
# IFUKwZA3X+CBJNeAeBIuYJPBBsBo4Ft42rz8BsSoUL+GMA0GCSqGSIb3DQEBAQUA
# BIIBAHOjJHGZPv+8u7ER+pMuPqAWXbIinytIlajNG6TL/ywxH0//3+VIds12me2h
# xAmMTrTNK9cKlrN+MEwkAe27EoiWQ4XxMcJVSwV2gqyL5csfwyznkbW5mYkwYTtn
# Kvo+BHOkoiRRdm4dQL9OOHC1KPYSguOBua99NKDBO7yD3ZoL1DpOXUL5fZ2QAqCc
# HfdWYK/+rE5DipR0p1rHtHA938vfTftm1sCPgrZasUnQJqG/2ly9oOUmw4ewh1yb
# 9IKbwbUmcPSI8DES05Eec3GwZNN5WxSILXs2vcyGdISGC85yI7+6x52H6GR5w60k
# hkdNJD5WA3rRGTOg+O7RIHgpeeChggIPMIICCwYJKoZIhvcNAQkGMYIB/DCCAfgC
# AQEwdjBiMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYD
# VQQLExB3d3cuZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBBc3N1cmVk
# IElEIENBLTECEAMBmgI6/1ixa9bV6uYX8GYwCQYFKw4DAhoFAKBdMBgGCSqGSIb3
# DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTE2MDQwNjA1MDMzNVow
# IwYJKoZIhvcNAQkEMRYEFC5lWCJagrE3eGSUgL3z43u21youMA0GCSqGSIb3DQEB
# AQUABIIBAEfJ5VgtxB6AeiRTZmDajUdkygtn5X2iDiJfEdRViPsBpeWEgxu/bp1u
# NcctDTCq0bg7y/VfsWLVAPwET5gia2Kn6vbJHx6HCoAwsgLmTcWW7dIR+Nfkg8Td
# 7uhAhPR5IEXHykea7y+iQ8GU3UrW87zXVu8RUDFIMZ7Qt3SlBDLNOIt1TyX4MUH/
# Mhv1Y+ReANIpLs8hDn8SzK0yfttjfn6Z/is26lmRpdxZe4g6HOjuA4wgCkyEv6Jr
# upuVvH0NmW13mxjZJ+JBGpFc7VGhV9MyJCVKiVubCJ7dzKH9dMKdg0gy1L0dc7O4
# B/R2IAwA6Sm39xZtCdC+6g8zUUdUzPM=
# SIG # End signature block
