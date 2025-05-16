pipeline {
    agent any
    
    environment {
        DOCKER_REGISTRY = 'docker.io'
        DOCKER_CREDENTIALS_ID = 'docker-registry-credentials'
        KUBE_CONFIG_ID = 'kubeconfig-credentials'
        GIT_CREDENTIALS_ID = 'git-credentials'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout([$class: 'GitSCM', 
                    branches: [[name: '*/master']], 
                    userRemoteConfigs: [[
                        credentialsId: "${GIT_CREDENTIALS_ID}",
                        url: 'https://github.com/kareemtoson12/cloudProjectHealth.git'
                    ]]
                ])
            }
        }
        
        stage('Build and Test') {
            parallel {
                stage('Patient Management API') {
                    steps {
                        dir('PatientManagement.API') {
                            sh 'dotnet build --verbosity detailed'
                            script {
                                def testProjects = sh(script: 'find . -name "*.Tests.csproj"', returnStdout: true).trim()
                                if (testProjects) {
                                    sh 'dotnet test --verbosity detailed --logger "console;verbosity=detailed"'
                                } else {
                                    echo 'No test projects found for Patient Management API'
                                }
                            }
                        }
                    }
                }
                stage('EHR API') {
                    steps {
                        dir('EHR.API') {
                            sh 'dotnet build --verbosity detailed'
                            script {
                                def testProjects = sh(script: 'find . -name "*.Tests.csproj"', returnStdout: true).trim()
                                if (testProjects) {
                                    sh 'dotnet test --verbosity detailed --logger "console;verbosity=detailed"'
                                } else {
                                    echo 'No test projects found for EHR API'
                                }
                            }
                        }
                    }
                }
                stage('Appointment Scheduling API') {
                    steps {
                        dir('AppointmentScheduling.API') {
                            sh 'dotnet build --verbosity detailed'
                            script {
                                def testProjects = sh(script: 'find . -name "*.Tests.csproj"', returnStdout: true).trim()
                                if (testProjects) {
                                    sh 'dotnet test --verbosity detailed --logger "console;verbosity=detailed"'
                                } else {
                                    echo 'No test projects found for Appointment Scheduling API'
                                }
                            }
                        }
                    }
                }
            }
        }
        
        stage('Build Docker Images') {
            steps {
                script {
                    def services = [
                        'patient-management-api',
                        'ehr-api',
                        'appointment-scheduling-api'
                    ]
                    
                    services.each { service ->
                        def imageName = "${DOCKER_REGISTRY}/${service}:${BUILD_NUMBER}"
                        sh "docker build -t ${imageName} -f ${service}/Dockerfile ."
                    }
                }
            }
        }
        
        stage('Security Scan') {
            steps {
                script {
                    def services = [
                        'patient-management-api',
                        'ehr-api',
                        'appointment-scheduling-api'
                    ]
                    
                    services.each { service ->
                        def imageName = "${DOCKER_REGISTRY}/${service}:${BUILD_NUMBER}"
                        sh "trivy image ${imageName}"
                    }
                }
            }
        }
        
        stage('Push Docker Images') {
            steps {
                withCredentials([usernamePassword(credentialsId: DOCKER_CREDENTIALS_ID, 
                                               usernameVariable: 'DOCKER_USER', 
                                               passwordVariable: 'DOCKER_PASS')]) {
                    sh 'echo $DOCKER_PASS | docker login ${DOCKER_REGISTRY} -u $DOCKER_USER --password-stdin'
                    
                    script {
                        def services = [
                            'patient-management-api',
                            'ehr-api',
                            'appointment-scheduling-api'
                        ]
                        
                        services.each { service ->
                            def imageName = "${DOCKER_REGISTRY}/${service}:${BUILD_NUMBER}"
                            sh "docker push ${imageName}"
                            sh "docker tag ${imageName} ${DOCKER_REGISTRY}/${service}:latest"
                            sh "docker push ${DOCKER_REGISTRY}/${service}:latest"
                        }
                    }
                }
            }
        }
        
        stage('Deploy to Kubernetes') {
            steps {
                withKubeConfig([credentialsId: KUBE_CONFIG_ID]) {
                    script {
                        // Update image versions in Kubernetes manifests
                        sh '''
                            cd k8s/base
                            for file in *.yaml; do
                                sed -i "s|image: ${DOCKER_REGISTRY}/.*|image: ${DOCKER_REGISTRY}/$(basename $file .yaml):${BUILD_NUMBER}|g" $file
                            done
                        '''
                        
                        // Apply Kubernetes configurations
                        sh 'kubectl apply -k k8s/base'
                        
                        // Verify deployment
                        sh '''
                            kubectl rollout status deployment/patient-management-api
                            kubectl rollout status deployment/ehr-api
                            kubectl rollout status deployment/appointment-scheduling-api
                        '''
                    }
                }
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
        success {
            echo 'Pipeline completed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
            script {
                def buildLog = currentBuild.rawBuild.getLog(1000).join('\n')
                echo "Build Log:\n${buildLog}"
            }
        }
    }
} 