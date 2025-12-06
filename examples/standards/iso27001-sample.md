# ISO 27001:2013 - Selected Requirements

**Standard:** ISO/IEC 27001:2013
**Version:** 2013
**Description:** Information Security Management System Requirements

This document contains selected requirements from ISO 27001:2013 Annex A controls for demonstration purposes.

## A.9 Access Control

### A.9.1.1: Access control policy

Access control rules and rights for each user or group of users shall be clearly stated in an access control policy.

**Control Objective:** To limit access to information and information processing facilities.

**Implementation Guidance:**
- Document formal access control policy
- Define access rights based on job requirements
- Implement least privilege principle
- Review and update policy regularly

### A.9.1.2: Access to networks and network services

Users shall only be provided with access to networks and network services that they have been specifically authorized to use.

**Control Objective:** To prevent unauthorized access to networks and network services.

**Implementation Guidance:**
- Implement network segmentation
- Use firewalls and access control lists
- Authenticate users before granting network access
- Monitor network access

### A.9.2.1: User registration and deregistration

A formal user registration and deregistration process shall be implemented to enable assignment of access rights.

**Control Objective:** To ensure authorized user access and prevent unauthorized access by former users.

**Implementation Guidance:**
- Formal approval process for new accounts
- Unique user identifiers
- Timely deactivation of accounts when no longer needed
- Review of user access rights

### A.9.2.3: Management of privileged access rights

The allocation and use of privileged access rights shall be restricted and controlled.

**Control Objective:** To prevent unauthorized access using privileged accounts.

**Implementation Guidance:**
- Limit number of privileged accounts
- Implement additional authentication for privileged access
- Log and monitor privileged activities
- Regular review of privileged access

### A.9.4.1: Information access restriction

Access to information and application system functions shall be restricted in accordance with the access control policy.

**Control Objective:** To prevent unauthorized access to information.

**Implementation Guidance:**
- Implement role-based access control
- Menu-based interfaces for specific functions
- Secure system utilities access
- Application-level access controls

### A.9.4.2: Secure log-on procedures

Where required by the access control policy, access to systems and applications shall be controlled by a secure log-on procedure.

**Control Objective:** To prevent unauthorized access through secure authentication.

**Implementation Guidance:**
- Display minimal information before logon
- Validate login information before granting access
- Limit unsuccessful logon attempts
- Force password change if compromise suspected
- Log successful and failed logon attempts

## A.10 Cryptography

### A.10.1.1: Policy on the use of cryptographic controls

A policy on the use of cryptographic controls shall be developed and implemented.

**Control Objective:** To ensure proper and effective use of cryptography.

**Implementation Guidance:**
- Define when cryptography should be used
- Specify encryption algorithms and key lengths
- Key management approach
- Roles and responsibilities for key management

### A.10.1.2: Key management

A policy on the use, protection and lifetime of cryptographic keys shall be developed and implemented.

**Control Objective:** To protect cryptographic keys throughout their lifecycle.

**Implementation Guidance:**
- Key generation procedures
- Secure key storage and distribution
- Key rotation and revocation procedures
- Key destruction when no longer needed

## A.12 Operations Security

### A.12.4.1: Event logging

Event logs recording user activities, exceptions, faults and information security events shall be produced, kept and regularly reviewed.

**Control Objective:** To record events and generate evidence.

**Implementation Guidance:**
- Log user IDs, dates, times of key events
- Log successful and unsuccessful access attempts
- Log privileged operations
- Protect logs from tampering
- Review logs regularly

### A.12.4.2: Protection of log information

Logging facilities and log information shall be protected against tampering and unauthorized access.

**Control Objective:** To protect the integrity of logs.

**Implementation Guidance:**
- Restrict access to log files
- Protect against unauthorized changes
- Store logs on separate systems if possible
- Define log retention periods

### A.12.6.1: Management of technical vulnerabilities

Information about technical vulnerabilities of information systems shall be obtained, exposure to such vulnerabilities evaluated, and appropriate measures taken.

**Control Objective:** To prevent exploitation of vulnerabilities.

**Implementation Guidance:**
- Timely awareness of vulnerabilities
- Assess risk and take corrective action
- Patch management procedures
- Track security patches
- Test patches before deployment

## A.14 System Acquisition, Development and Maintenance

### A.14.2.1: Secure development policy

Rules for the development of software and systems shall be established and applied to developments within the organization.

**Control Objective:** To ensure security is built into information systems.

**Implementation Guidance:**
- Security requirements in development lifecycle
- Secure coding standards
- Security testing requirements
- Change control procedures

### A.14.2.5: Secure system engineering principles

Principles for engineering secure systems shall be established, documented and maintained.

**Control Objective:** To ensure systems are designed and implemented securely.

**Implementation Guidance:**
- Defense in depth
- Fail secure principles
- Least privilege by default
- Security architecture reviews

## A.18 Compliance

### A.18.1.1: Identification of applicable legislation and contractual requirements

All relevant legislative statutory, regulatory, contractual requirements and the organization's approach to meet these requirements shall be explicitly identified, documented and kept up to date.

**Control Objective:** To avoid breaches of legal, statutory, regulatory or contractual obligations.

**Implementation Guidance:**
- Identify all applicable legal requirements
- Document compliance requirements
- Define procedures to meet requirements
- Review regularly for changes

### A.18.1.3: Protection of records

Records shall be protected from loss, destruction, falsification, unauthorized access and unauthorized release.

**Control Objective:** To ensure important records are protected.

**Implementation Guidance:**
- Define retention requirements
- Implement secure storage
- Protection against loss and destruction
- Define disposal procedures
