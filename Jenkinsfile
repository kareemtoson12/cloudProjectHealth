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
        
        stage('Build Solution') {
            steps {
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
            }
        }
        
        stage('Run Tests') {
            steps {
                script {
                    def testProjects = sh(script: 'find . -name "*.Tests.csproj"', returnStdout: true).trim()
                    if (testProjects) {
                        sh 'dotnet test --no-build --verbosity detailed'
                    } else {
                        echo 'No test projects found'
                    }
                }
            }
        }
        
        stage('Build Docker Images') {
            steps {
                script {
                    def services = [
                        'PatientManagement.API',
                        'EHR.API',
                        'AppointmentScheduling.API'
                    ]
                    
                    services.each { service ->
                        def imageName = "${DOCKER_REGISTRY}/${service.toLowerCase()}:${BUILD_NUMBER}"
                        sh "docker build -t ${imageName} -f ${service}/Dockerfile ."
                    }
                }
            }
        }
        
        stage('Security Scan') {
            steps {
                script {
                    def services = [
                        'PatientManagement.API',
                        'EHR.API',
                        'AppointmentScheduling.API'
                    ]
                    
                    services.each { service ->
                        def imageName = "${DOCKER_REGISTRY}/${service.toLowerCase()}:${BUILD_NUMBER}"
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
                            'PatientManagement.API',
                            'EHR.API',
                            'AppointmentScheduling.API'
                        ]
                        
                        services.each { service ->
                            def imageName = "${DOCKER_REGISTRY}/${service.toLowerCase()}:${BUILD_NUMBER}"
                            sh "docker push ${imageName}"
                            sh "docker tag ${imageName} ${DOCKER_REGISTRY}/${service.toLowerCase()}:latest"
                            sh "docker push ${DOCKER_REGISTRY}/${service.toLowerCase()}:latest"
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