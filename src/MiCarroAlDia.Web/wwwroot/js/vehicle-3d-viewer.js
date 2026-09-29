/**
 * Mi Carro al Día - Visor 3D Realista de Vehículo y Diagnóstico Automotriz
 * Renderizado de alta fidelidad con Three.js:
 * - Carrocería esculpida aerodinámica (perfil de auto real, guardabarros, capó, cabina, alerón)
 * - Iluminación de estudio profesional con mapa de reflejos procedural (PBR / Clearcoat)
 * - Rines deportivos de 5 radios dobles, neumáticos con perfil y frenos de disco ventilados con mordazas rojas
 * - Faros LED con proyectores y lentes de policarbonato, pilotos traseros LED
 * - Elevador hidráulico industrial detallado de 2 columnas con cilindros cromados y brazos articulados
 * - Pines de inspección discretos y elegantes tipo configurador de alta gama (solo expanden al interactuar)
 */

class Vehicle3DViewer {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) {
            console.error('Vehicle3DViewer: Contenedor no encontrado:', containerId);
            return;
        }

        this.options = Object.assign({
            vehiclePlate: 'ABC-123',
            vehicleModel: 'Renault Sandero 1.6',
            currentState: 'Diagnostico',
            items: [],
            isLiftElevated: false,
            interactive: true
        }, options);

        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.carGroup = null;
        this.liftGroup = null;
        this.liftArmsGroup = null;
        this.hotspots = [];
        this.highlightMeshes = {};
        this.animatingCamera = false;
        this.cameraTargetPos = null;
        this.controlsTargetPos = null;
        this.currentLiftHeight = 0;
        this.targetLiftHeight = (this.options.currentState === 'Diagnostico' || this.options.currentState === 'Reparacion') ? 0.95 : 0;
        this.isLiftElevated = this.targetLiftHeight > 0;
        this.activePartKey = null;

        this.init();
    }

    init() {
        if (typeof THREE === 'undefined') {
            console.error('Vehicle3DViewer: Three.js no está cargado');
            this.container.innerHTML = '<div class="alert alert-warning m-3">Cargando visor 3D...</div>';
            return;
        }

        this.setupRenderer();
        this.setupScene();
        this.setupEnvironmentMap();
        this.setupCamera();
        this.setupLights();
        this.buildWorkshopStudio();
        this.buildSculptedRealisticCar();
        this.buildIndustrialLift();
        this.setupHotspots();
        this.setupEvents();
        this.animate();

        if (this.targetLiftHeight > 0) {
            this.setLiftElevated(true, false);
        }
    }

    setupRenderer() {
        this.container.innerHTML = '';
        this.container.style.position = 'relative';
        this.container.style.overflow = 'hidden';

        const width = this.container.clientWidth || 600;
        const height = this.container.clientHeight || 380;

        this.renderer = new THREE.WebGLRenderer({
            antialias: true,
            alpha: true,
            powerPreference: 'high-performance'
        });
        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

        if (THREE.ACESFilmicToneMapping) {
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.renderer.toneMappingExposure = 1.15;
        }

        this.container.appendChild(this.renderer.domElement);

        // Capa de pines 3D (Hotspots interactivos)
        this.hotspotsOverlay = document.createElement('div');
        this.hotspotsOverlay.className = 'vehicle-hotspots-overlay';
        this.container.appendChild(this.hotspotsOverlay);
    }

    setupScene() {
        this.scene = new THREE.Scene();
        this.scene.fog = new THREE.FogExp2(0x0a0f1d, 0.035);
    }

    /**
     * Genera un mapa de entorno HDR procedural de estudio fotográfico automotriz
     * Esto otorga reflejos curvos realistas en la pintura y cristales del vehículo
     */
    setupEnvironmentMap() {
        if (!THREE.PMREMGenerator) return;

        const pmremGenerator = new THREE.PMREMGenerator(this.renderer);
        pmremGenerator.compileEquirectangularShader();

        const studioScene = new THREE.Scene();
        studioScene.background = new THREE.Color(0x0f172a);

        // Softbox superior principal (luz cenital de estudio fotográfico)
        const topSoftboxGeo = new THREE.PlaneGeometry(8, 14);
        const topSoftboxMat = new THREE.MeshBasicMaterial({ color: 0xffffff });
        const topSoftbox = new THREE.Mesh(topSoftboxGeo, topSoftboxMat);
        topSoftbox.position.set(0, 6, 0);
        topSoftbox.rotation.x = Math.PI / 2;
        studioScene.add(topSoftbox);

        // Softbox lateral derecho (reflejo de perfil cromado)
        const sideSoftboxR = new THREE.Mesh(
            new THREE.PlaneGeometry(3, 12),
            new THREE.MeshBasicMaterial({ color: 0xe0f2fe })
        );
        sideSoftboxR.position.set(5.5, 3.5, 0);
        sideSoftboxR.rotation.y = -Math.PI / 2;
        studioScene.add(sideSoftboxR);

        // Softbox lateral izquierdo (relleno suave azul)
        const sideSoftboxL = new THREE.Mesh(
            new THREE.PlaneGeometry(3, 12),
            new THREE.MeshBasicMaterial({ color: 0x38bdf8 })
        );
        sideSoftboxL.position.set(-5.5, 3.5, 0);
        sideSoftboxL.rotation.y = Math.PI / 2;
        studioScene.add(sideSoftboxL);

        // Horizonte cálido sutil
        const horizonGeo = new THREE.CylinderGeometry(15, 15, 6, 32, 1, true);
        const horizonMat = new THREE.MeshBasicMaterial({ color: 0x1e293b, side: THREE.BackSide });
        const horizonMesh = new THREE.Mesh(horizonGeo, horizonMat);
        horizonMesh.position.y = 2;
        studioScene.add(horizonMesh);

        const envMap = pmremGenerator.fromScene(studioScene, 0.04).texture;
        this.scene.environment = envMap;
        pmremGenerator.dispose();
    }

    setupCamera() {
        const width = this.container.clientWidth || 600;
        const height = this.container.clientHeight || 380;
        this.camera = new THREE.PerspectiveCamera(38, width / height, 0.1, 100);
        this.camera.position.set(3.5, 1.7, 3.7);

        if (typeof THREE.OrbitControls !== 'undefined') {
            this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.dampingFactor = 0.06;
            this.controls.minDistance = 2.0;
            this.controls.maxDistance = 8.0;
            this.controls.maxPolarAngle = Math.PI / 2 - 0.03; // No bajar del piso
            this.controls.target.set(0, 0.65, 0);
            this.controls.update();
        }
    }

    setupLights() {
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.85);
        this.scene.add(ambientLight);

        // Luz principal tipo sol con sombras suaves
        const keyLight = new THREE.DirectionalLight(0xffffff, 1.35);
        keyLight.position.set(4, 7, 4.5);
        keyLight.castShadow = true;
        keyLight.shadow.mapSize.width = 1024;
        keyLight.shadow.mapSize.height = 1024;
        keyLight.shadow.camera.near = 0.5;
        keyLight.shadow.camera.far = 18;
        keyLight.shadow.camera.left = -3.5;
        keyLight.shadow.camera.right = 3.5;
        keyLight.shadow.camera.top = 3.5;
        keyLight.shadow.camera.bottom = -3.5;
        keyLight.shadow.bias = -0.0008;
        this.scene.add(keyLight);

        // Luz de contorno trasera (rim light para resaltar aristas de la carrocería)
        const rimLight = new THREE.DirectionalLight(0x38bdf8, 0.9);
        rimLight.position.set(-4, 4, -4);
        this.scene.add(rimLight);

        // Luz frontal de relleno
        const frontLight = new THREE.DirectionalLight(0xfffbeb, 0.65);
        frontLight.position.set(0, 2.5, 5);
        this.scene.add(frontLight);
    }

    buildWorkshopStudio() {
        // Piso de taller epóxico reflectivo oscuro
        const floorGeo = new THREE.PlaneGeometry(24, 24);
        const floorMat = new THREE.MeshStandardMaterial({
            color: 0x0b1120,
            roughness: 0.25,
            metalness: 0.35
        });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.receiveShadow = true;
        this.scene.add(floor);

        // Líneas sutiles de demarcación de puesto de trabajo
        const bayLineGeo = new THREE.RingGeometry(2.4, 2.45, 48);
        const bayLineMat = new THREE.MeshBasicMaterial({ color: 0x0284c7, transparent: true, opacity: 0.4 });
        const bayRing = new THREE.Mesh(bayLineGeo, bayLineMat);
        bayRing.rotation.x = -Math.PI / 2;
        bayRing.position.y = 0.003;
        this.scene.add(bayRing);

        // Sombra de contacto realista difusa bajo el vehículo
        const shadowCanvas = document.createElement('canvas');
        shadowCanvas.width = 256;
        shadowCanvas.height = 256;
        const ctx = shadowCanvas.getContext('2d');
        const grad = ctx.createRadialGradient(128, 128, 20, 128, 128, 120);
        grad.addColorStop(0, 'rgba(0,0,0,0.85)');
        grad.addColorStop(0.5, 'rgba(0,0,0,0.45)');
        grad.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, 256, 256);

        const shadowTexture = new THREE.CanvasTexture(shadowCanvas);
        const shadowGeo = new THREE.PlaneGeometry(4.6, 2.5);
        const shadowMat = new THREE.MeshBasicMaterial({
            map: shadowTexture,
            transparent: true,
            opacity: 0.75,
            depthWrite: false
        });
        this.contactShadow = new THREE.Mesh(shadowGeo, shadowMat);
        this.contactShadow.rotation.x = -Math.PI / 2;
        this.contactShadow.position.y = 0.006;
        this.scene.add(this.contactShadow);
    }

    /**
     * Construcción de carrocería estilizada con perfil aerodinámico de vehículo real
     * Emula un compacto/hatchback crossover moderno (Renault Sandero / Onix)
     */
    buildSculptedRealisticCar() {
        this.carGroup = new THREE.Group();

        // 1. MATERIALES PBR REALISTAS
        // Pintura automotriz azul zafiro con brillo tipo barniz de laca
        const carPaintMat = new THREE.MeshStandardMaterial({
            color: 0x1d4ed8, // Azul royal Autofrenos
            metalness: 0.88,
            roughness: 0.18,
            envMapIntensity: 1.4
        });
        this.carPaintMaterial = carPaintMat;

        // Molduras inferiores y pasos de rueda en negro texturizado crossover
        const claddingMat = new THREE.MeshStandardMaterial({
            color: 0x111827,
            metalness: 0.15,
            roughness: 0.75
        });

        // Vidrio automotriz con tinte y alto reflejo
        const glassMat = new THREE.MeshStandardMaterial({
            color: 0x0f172a,
            metalness: 0.4,
            roughness: 0.05,
            transparent: true,
            opacity: 0.82,
            envMapIntensity: 2.0
        });

        // Cromo brillante pulido para detalles y parrilla
        const chromeMat = new THREE.MeshStandardMaterial({
            color: 0xf8fafc,
            metalness: 0.98,
            roughness: 0.08,
            envMapIntensity: 2.2
        });

        // Faros LED de alta potencia
        const ledHeadlightMat = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            emissive: 0xe0f2fe,
            emissiveIntensity: 1.5,
            roughness: 0.1
        });

        // Pilotos traseros LED con luz roja cristalina
        const ledTailMat = new THREE.MeshStandardMaterial({
            color: 0xdc2626,
            emissive: 0xef4444,
            emissiveIntensity: 1.6,
            roughness: 0.15
        });

        // 2. CHASIS Y CARROCERÍA INFERIOR ESCULPIDA
        // Perfil lateral del chasis extruido con curva suave
        const bodyShape = new THREE.Shape();
        // Empezamos en la parte delantera inferior (frente a rueda delantera)
        bodyShape.moveTo(0, 0.22);
        bodyShape.lineTo(0.55, 0.22); // hasta borde de rueda delantera
        bodyShape.absarc(0.95, 0.22, 0.40, Math.PI, 0, true); // arco rueda delantera
        bodyShape.lineTo(2.0, 0.22); // zócalo entre ruedas
        bodyShape.absarc(2.4, 0.22, 0.40, Math.PI, 0, true); // arco rueda trasera
        bodyShape.lineTo(3.2, 0.22); // hasta paragolpes trasero
        bodyShape.lineTo(3.3, 0.45); // cola trasera
        bodyShape.lineTo(3.2, 0.75); // portón trasero
        bodyShape.lineTo(2.5, 0.82); // borde luneta trasera
        bodyShape.lineTo(0.9, 0.82); // línea de cintura hacia capó
        bodyShape.lineTo(0.05, 0.65); // punta delantera de capó
        bodyShape.lineTo(0.0, 0.45); // rejilla delantera
        bodyShape.closePath();

        const extrudeSettings = {
            steps: 2,
            depth: 1.62,
            bevelEnabled: true,
            bevelThickness: 0.06,
            bevelSize: 0.05,
            bevelSegments: 4
        };

        const mainBodyGeo = new THREE.ExtrudeGeometry(bodyShape, extrudeSettings);
        // Centrar geometría en el eje X y Z
        mainBodyGeo.center();
        const mainBodyMesh = new THREE.Mesh(mainBodyGeo, carPaintMat);
        mainBodyMesh.rotation.y = Math.PI / 2;
        mainBodyMesh.position.set(0, 0.46, 0);
        mainBodyMesh.castShadow = true;
        mainBodyMesh.receiveShadow = true;
        this.carGroup.add(mainBodyMesh);

        // 3. CABINA / TECHO Y CRISTALES AERODINÁMICOS
        // Perfil de la cabina (inclinación parabrisas, arco de techo, caída de luneta)
        const cabinShape = new THREE.Shape();
        cabinShape.moveTo(0.0, 0.0);
        cabinShape.lineTo(0.70, 0.52); // Parabrisas inclinado hacia techo
        cabinShape.lineTo(1.85, 0.52); // Techo horizontal
        cabinShape.lineTo(2.35, 0.0);  // Luneta trasera inclinada
        cabinShape.closePath();

        const cabinGeo = new THREE.ExtrudeGeometry(cabinShape, {
            steps: 2,
            depth: 1.38,
            bevelEnabled: true,
            bevelThickness: 0.05,
            bevelSize: 0.04,
            bevelSegments: 3
        });
        cabinGeo.center();
        const cabinMesh = new THREE.Mesh(cabinGeo, glassMat);
        cabinMesh.rotation.y = Math.PI / 2;
        cabinMesh.position.set(0, 0.98, -0.15);
        cabinMesh.castShadow = true;
        this.carGroup.add(cabinMesh);

        // Pilares B y C de la cabina en negro brillante (acabado automotriz)
        const pillarGeo = new THREE.BoxGeometry(1.42, 0.48, 0.08);
        const pillarMat = new THREE.MeshStandardMaterial({ color: 0x090d16, roughness: 0.4, metalness: 0.2 });
        const pillarB = new THREE.Mesh(pillarGeo, pillarMat);
        pillarB.position.set(0, 0.96, -0.12);
        this.carGroup.add(pillarB);

        // Barras de techo estilo crossover / SUV en cromo y negro
        const roofRailGeo = new THREE.CylinderGeometry(0.02, 0.02, 1.4, 8);
        roofRailGeo.rotateX(Math.PI / 2);
        const roofRailR = new THREE.Mesh(roofRailGeo, chromeMat);
        roofRailR.position.set(0.62, 1.25, -0.12);
        const roofRailL = new THREE.Mesh(roofRailGeo, chromeMat);
        roofRailL.position.set(-0.62, 1.25, -0.12);
        this.carGroup.add(roofRailR, roofRailL);

        // Alerón trasero sutil en la parte superior del portón
        const spoilerGeo = new THREE.BoxGeometry(1.36, 0.05, 0.22);
        const spoiler = new THREE.Mesh(spoilerGeo, carPaintMat);
        spoiler.position.set(0, 1.22, -1.25);
        this.carGroup.add(spoiler);

        // 4. PARAGOLPES Y FRONTAL REALISTA
        // Paragolpes delantero esculpido con difusor inferior
        const frontBumperGeo = new THREE.BoxGeometry(1.66, 0.30, 0.32);
        const frontBumper = new THREE.Mesh(frontBumperGeo, claddingMat);
        frontBumper.position.set(0, 0.36, 1.82);
        this.carGroup.add(frontBumper);

        // Parrilla frontal tipo panal con reborde cromado
        const grillGeo = new THREE.BoxGeometry(1.08, 0.16, 0.06);
        const grill = new THREE.Mesh(grillGeo, new THREE.MeshStandardMaterial({ color: 0x111827, roughness: 0.85 }));
        grill.position.set(0, 0.45, 1.96);
        this.carGroup.add(grill);

        // Moldura cromada horizontal en la parrilla
        const grillChrome = new THREE.Mesh(new THREE.BoxGeometry(1.12, 0.03, 0.08), chromeMat);
        grillChrome.position.set(0, 0.46, 1.96);
        this.carGroup.add(grillChrome);

        // Rombo / Emblema central estilizado
        const badgeGeo = new THREE.OctahedronGeometry(0.08, 0);
        const badge = new THREE.Mesh(badgeGeo, chromeMat);
        badge.position.set(0, 0.48, 1.99);
        badge.rotation.y = Math.PI / 4;
        this.carGroup.add(badge);

        // Faros delanteros modernos esculpidos con luz diurna DRL
        const headlightGeo = new THREE.BoxGeometry(0.36, 0.14, 0.18);
        const headlightR = new THREE.Mesh(headlightGeo, ledHeadlightMat);
        headlightR.position.set(0.62, 0.58, 1.86);
        headlightR.rotation.y = -0.15;
        const headlightL = new THREE.Mesh(headlightGeo, ledHeadlightMat);
        headlightL.position.set(-0.62, 0.58, 1.86);
        headlightL.rotation.y = 0.15;
        this.carGroup.add(headlightR, headlightL);

        // Pilotos traseros esculpidos envolventes
        const taillightGeo = new THREE.BoxGeometry(0.38, 0.15, 0.12);
        const taillightR = new THREE.Mesh(taillightGeo, ledTailMat);
        taillightR.position.set(0.62, 0.64, -1.82);
        const taillightL = new THREE.Mesh(taillightGeo, ledTailMat);
        taillightL.position.set(-0.62, 0.64, -1.82);
        this.carGroup.add(taillightR, taillightL);

        // Placa trasera colombiana estilizada
        const plateGeo = new THREE.PlaneGeometry(0.38, 0.18);
        const plateCanvas = document.createElement('canvas');
        plateCanvas.width = 128;
        plateCanvas.height = 64;
        const pCtx = plateCanvas.getContext('2d');
        pCtx.fillStyle = '#fdd835';
        pCtx.fillRect(0, 0, 128, 64);
        pCtx.lineWidth = 4;
        pCtx.strokeStyle = '#000000';
        pCtx.strokeRect(2, 2, 124, 60);
        pCtx.fillStyle = '#000000';
        pCtx.font = 'bold 24px monospace';
        pCtx.textAlign = 'center';
        pCtx.fillText(this.options.vehiclePlate || 'ABC-123', 64, 40);

        const plateTex = new THREE.CanvasTexture(plateCanvas);
        const plateMat = new THREE.MeshBasicMaterial({ map: plateTex });
        const rearPlate = new THREE.Mesh(plateGeo, plateMat);
        rearPlate.position.set(0, 0.44, -1.89);
        rearPlate.rotation.y = Math.PI;
        this.carGroup.add(rearPlate);

        // Espejos retrovisores esculpidos
        const mirrorGeo = new THREE.BoxGeometry(0.18, 0.10, 0.14);
        const mirrorR = new THREE.Mesh(mirrorGeo, carPaintMat);
        mirrorR.position.set(0.86, 0.82, 0.55);
        const mirrorL = new THREE.Mesh(mirrorGeo, carPaintMat);
        mirrorL.position.set(-0.86, 0.82, 0.55);
        this.carGroup.add(mirrorR, mirrorL);

        // 5. LIMPIAPARABRISAS ARTICULADOS
        const wiperGroup = new THREE.Group();
        const wiperArmMat = new THREE.MeshStandardMaterial({ color: 0x1f2937, roughness: 0.6, metalness: 0.8 });
        const wiperGeo = new THREE.CylinderGeometry(0.012, 0.012, 0.48, 6);
        wiperGeo.rotateZ(Math.PI / 2);

        const wiper1 = new THREE.Mesh(wiperGeo, wiperArmMat);
        wiper1.position.set(0.32, 0.74, 0.86);
        wiper1.rotation.y = 0.35;
        wiper1.rotation.x = -0.65;

        const wiper2 = new THREE.Mesh(wiperGeo, wiperArmMat);
        wiper2.position.set(-0.25, 0.74, 0.86);
        wiper2.rotation.y = 0.35;
        wiper2.rotation.x = -0.65;

        wiperGroup.add(wiper1, wiper2);
        this.carGroup.add(wiperGroup);
        this.highlightMeshes['wipers'] = wiperGroup;

        // 6. RUEDAS REALISTAS: RINES DE ALEACIÓN Y FRENOS DE DISCO VISIBLES
        this.buildRealisticWheels();

        // 7. COMPARTIMIENTO DEL MOTOR BAJO EL CAPÓ
        this.buildEngineBay();

        this.scene.add(this.carGroup);
    }

    /**
     * Construye rines deportivos de 5 radios dobles con neumáticos ranurados y
     * discos de freno ventilados de acero perforado con pinzas rojas de alto desempeño
     */
    buildRealisticWheels() {
        const wheelPositions = [
            { x: 0.82, y: 0.34, z: 1.15, isFront: true, side: 'R' },
            { x: -0.82, y: 0.34, z: 1.15, isFront: true, side: 'L' },
            { x: 0.82, y: 0.34, z: -1.15, isFront: false, side: 'R' },
            { x: -0.82, y: 0.34, z: -1.15, isFront: false, side: 'L' }
        ];

        // Neumático con hombro redondeado
        const tireGeo = new THREE.CylinderGeometry(0.34, 0.34, 0.22, 32);
        tireGeo.rotateZ(Math.PI / 2);
        const tireMat = new THREE.MeshStandardMaterial({
            color: 0x14181f,
            roughness: 0.92,
            metalness: 0.05
        });

        // Aro exterior del rin
        const rimOuterGeo = new THREE.CylinderGeometry(0.24, 0.24, 0.225, 24);
        rimOuterGeo.rotateZ(Math.PI / 2);
        const rimAlloyMat = new THREE.MeshStandardMaterial({
            color: 0xe2e8f0,
            metalness: 0.94,
            roughness: 0.15,
            envMapIntensity: 2.0
        });

        // Disco de freno de acero inoxidable perforado
        const discGeo = new THREE.CylinderGeometry(0.20, 0.20, 0.02, 24);
        discGeo.rotateZ(Math.PI / 2);
        const discMat = new THREE.MeshStandardMaterial({
            color: 0x94a3b8,
            metalness: 0.96,
            roughness: 0.28,
            envMapIntensity: 1.5
        });

        // Pinza de freno deportiva (caliper rojo automotriz)
        const caliperGeo = new THREE.BoxGeometry(0.065, 0.13, 0.09);
        const caliperMat = new THREE.MeshStandardMaterial({
            color: 0xdc2626,
            roughness: 0.3,
            metalness: 0.5,
            emissive: 0x991b1b,
            emissiveIntensity: 0.35
        });

        // Resortes helicoidales de suspensión
        const springGeo = new THREE.CylinderGeometry(0.045, 0.045, 0.34, 12);
        const springMat = new THREE.MeshStandardMaterial({
            color: 0xf59e0b,
            metalness: 0.8,
            roughness: 0.3
        });

        const brakesGroup = new THREE.Group();
        const suspensionGroup = new THREE.Group();

        wheelPositions.forEach((wp) => {
            const wheelUnit = new THREE.Group();
            wheelUnit.position.set(wp.x, wp.y, wp.z);

            // Neumático
            const tire = new THREE.Mesh(tireGeo, tireMat);
            tire.castShadow = true;
            wheelUnit.add(tire);

            // Rin exterior
            const rimOuter = new THREE.Mesh(rimOuterGeo, rimAlloyMat);
            wheelUnit.add(rimOuter);

            // 5 Radios dobles esculpidos en el rin
            for (let i = 0; i < 5; i++) {
                const angle = (i * Math.PI * 2) / 5;
                const spokeGeo = new THREE.BoxGeometry(0.22, 0.03, 0.025);
                const spoke = new THREE.Mesh(spokeGeo, rimAlloyMat);
                spoke.rotation.x = angle;
                spoke.position.x = wp.side === 'R' ? 0.09 : -0.09;
                wheelUnit.add(spoke);
            }

            // Tapa central del cubo de rueda
            const hubGeo = new THREE.CylinderGeometry(0.06, 0.06, 0.04, 16);
            hubGeo.rotateZ(Math.PI / 2);
            const hub = new THREE.Mesh(hubGeo, rimAlloyMat);
            hub.position.x = wp.side === 'R' ? 0.10 : -0.10;
            wheelUnit.add(hub);

            // Disco de freno ventilado
            const innerX = wp.side === 'R' ? -0.05 : 0.05;
            const disc = new THREE.Mesh(discGeo, discMat.clone());
            disc.position.x = innerX;

            // Pinza de freno (Caliper)
            const caliper = new THREE.Mesh(caliperGeo, caliperMat.clone());
            caliper.position.set(innerX, 0.11, 0.04);

            wheelUnit.add(disc, caliper);
            this.carGroup.add(wheelUnit);

            if (wp.isFront) {
                brakesGroup.add(disc, caliper);
            }

            // Resorte de amortiguador en paso de rueda
            const strut = new THREE.Mesh(springGeo, springMat);
            strut.position.set(wp.x * 0.72, wp.y + 0.28, wp.z);
            suspensionGroup.add(strut);
        });

        this.carGroup.add(suspensionGroup);
        this.highlightMeshes['brakes'] = brakesGroup;
        this.highlightMeshes['suspension'] = suspensionGroup;
    }

    buildEngineBay() {
        const engineGroup = new THREE.Group();

        // Bloque motor
        const blockGeo = new THREE.BoxGeometry(0.72, 0.32, 0.58);
        const blockMat = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            metalness: 0.85,
            roughness: 0.35
        });
        const engineBlock = new THREE.Mesh(blockGeo, blockMat);
        engineBlock.position.set(0, 0.48, 1.25);

        // Tapa de válvulas superior con relieve de líneas
        const coverGeo = new THREE.BoxGeometry(0.68, 0.06, 0.48);
        const coverMat = new THREE.MeshStandardMaterial({ color: 0x334155, metalness: 0.9, roughness: 0.25 });
        const engineCover = new THREE.Mesh(coverGeo, coverMat);
        engineCover.position.set(0, 0.65, 1.25);

        // Batería con bornes rojo y negro
        const batteryGeo = new THREE.BoxGeometry(0.24, 0.20, 0.18);
        const battery = new THREE.Mesh(batteryGeo, new THREE.MeshStandardMaterial({ color: 0x0f172a, roughness: 0.8 }));
        battery.position.set(0.48, 0.56, 1.45);

        // Depósito lavaparabrisas con tapa azul
        const washerGeo = new THREE.CylinderGeometry(0.045, 0.045, 0.04, 12);
        const washerCap = new THREE.Mesh(washerGeo, new THREE.MeshStandardMaterial({ color: 0x0284c7 }));
        washerCap.position.set(-0.48, 0.65, 1.45);

        // Tapón de aceite amarillo
        const oilCapGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.03, 10);
        const oilCap = new THREE.Mesh(oilCapGeo, new THREE.MeshStandardMaterial({ color: 0xf59e0b }));
        oilCap.position.set(0.18, 0.69, 1.22);

        engineGroup.add(engineBlock, engineCover, battery, washerCap, oilCap);
        this.carGroup.add(engineGroup);
        this.highlightMeshes['engine'] = engineGroup;
    }

    /**
     * Elevador hidráulico industrial de 2 columnas de taller automotriz
     * Diseñado con perfiles de viga C, bases apernadas, pistones cromados y brazos simétricos
     */
    buildIndustrialLift() {
        this.liftGroup = new THREE.Group();
        this.liftArmsGroup = new THREE.Group();

        // Acabado amarillo seguridad industrial automotriz
        const safetyYellowMat = new THREE.MeshStandardMaterial({
            color: 0xd97706,
            metalness: 0.45,
            roughness: 0.4
        });

        // Acero oscuro maquinado
        const darkSteelMat = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            metalness: 0.85,
            roughness: 0.25
        });

        // Pistón hidráulico cromado brillante
        const chromeRodMat = new THREE.MeshStandardMaterial({
            color: 0xf1f5f9,
            metalness: 0.98,
            roughness: 0.05
        });

        const colHeight = 2.6;
        const colDistance = 1.42;

        // 2 Columnas industriales tipo viga con ranura interior
        const colGeo = new THREE.BoxGeometry(0.22, colHeight, 0.28);
        const basePlateGeo = new THREE.BoxGeometry(0.48, 0.04, 0.54);

        // Columna Derecha
        const colR = new THREE.Mesh(colGeo, safetyYellowMat);
        colR.position.set(colDistance, colHeight / 2, 0);
        colR.castShadow = true;
        const baseR = new THREE.Mesh(basePlateGeo, darkSteelMat);
        baseR.position.set(colDistance, 0.02, 0);
        this.liftGroup.add(colR, baseR);

        // Columna Izquierda
        const colL = new THREE.Mesh(colGeo, safetyYellowMat);
        colL.position.set(-colDistance, colHeight / 2, 0);
        colL.castShadow = true;
        const baseL = new THREE.Mesh(basePlateGeo, darkSteelMat);
        baseL.position.set(-colDistance, 0.02, 0);
        this.liftGroup.add(colL, baseL);

        // Carros deslizantes (Carriages) que suben por los rieles de cada columna
        const carriageGeo = new THREE.BoxGeometry(0.18, 0.35, 0.32);
        const carriageR = new THREE.Mesh(carriageGeo, darkSteelMat);
        carriageR.position.set(colDistance - 0.08, 0.18, 0);
        const carriageL = new THREE.Mesh(carriageGeo, darkSteelMat);
        carriageL.position.set(-colDistance + 0.08, 0.18, 0);

        // Cilindros hidráulicos verticales visibles en el mástil
        const rodGeo = new THREE.CylinderGeometry(0.025, 0.025, colHeight, 16);
        const rodR = new THREE.Mesh(rodGeo, chromeRodMat);
        rodR.position.set(colDistance - 0.04, colHeight / 2, 0);
        const rodL = new THREE.Mesh(rodGeo, chromeRodMat);
        rodL.position.set(-colDistance + 0.04, colHeight / 2, 0);
        this.liftGroup.add(rodR, rodL);

        // Brazos telescópicos articulados que calzan bajo los puntos de apoyo del chasis
        const armBarGeo = new THREE.BoxGeometry(0.72, 0.08, 0.10);
        const padGeo = new THREE.CylinderGeometry(0.075, 0.075, 0.04, 16);
        const rubberPadMat = new THREE.MeshStandardMaterial({ color: 0x090d16, roughness: 0.95 });

        // 4 Brazos de apoyo
        const armFR = new THREE.Mesh(armBarGeo, safetyYellowMat);
        armFR.position.set(0.85, 0.16, 0.50);
        armFR.rotation.y = 0.55;
        const padFR = new THREE.Mesh(padGeo, rubberPadMat);
        padFR.position.set(0.58, 0.21, 0.78);

        const armRR = new THREE.Mesh(armBarGeo, safetyYellowMat);
        armRR.position.set(0.85, 0.16, -0.50);
        armRR.rotation.y = -0.55;
        const padRR = new THREE.Mesh(padGeo, rubberPadMat);
        padRR.position.set(0.58, 0.21, -0.78);

        const armFL = new THREE.Mesh(armBarGeo, safetyYellowMat);
        armFL.position.set(-0.85, 0.16, 0.50);
        armFL.rotation.y = -0.55;
        const padFL = new THREE.Mesh(padGeo, rubberPadMat);
        padFL.position.set(-0.58, 0.21, 0.78);

        const armRL = new THREE.Mesh(armBarGeo, safetyYellowMat);
        armRL.position.set(-0.85, 0.16, -0.50);
        armRL.rotation.y = 0.55;
        const padRL = new THREE.Mesh(padGeo, rubberPadMat);
        padRL.position.set(-0.58, 0.21, -0.78);

        this.liftArmsGroup.add(carriageR, carriageL, armFR, armRR, armFL, armRL, padFR, padRR, padFL, padRL);
        this.scene.add(this.liftGroup);
        this.scene.add(this.liftArmsGroup);
    }

    setupHotspots() {
        const hasBrakes = this.options.items.some(i => i.name.toLowerCase().includes('freno') || i.name.toLowerCase().includes('pastilla') || i.name.toLowerCase().includes('disco'));
        const hasWipers = this.options.items.some(i => i.name.toLowerCase().includes('plumilla') || i.name.toLowerCase().includes('parabrisa'));
        const hasSuspension = this.options.items.some(i => i.name.toLowerCase().includes('amortiguador') || i.name.toLowerCase().includes('suspensión') || i.name.toLowerCase().includes('alineación'));

        this.hotspotsConfig = [
            {
                id: 'brakes',
                number: '1',
                label: 'Frenos Delanteros',
                subtitle: hasBrakes ? 'Pastillas y discos desgastados (Seguridad)' : 'Sistema de frenos verificado',
                worldPos: new THREE.Vector3(0.96, 0.44, 1.15),
                category: 'safety',
                hasQuote: hasBrakes,
                cameraPos: new THREE.Vector3(1.7, 0.75, 1.7),
                cameraLookAt: new THREE.Vector3(0.85, 0.35, 1.15)
            },
            {
                id: 'wipers',
                number: '2',
                label: 'Limpiaparabrisas',
                subtitle: hasWipers ? 'Plumillas de goma cristalizada' : 'Visibilidad óptima',
                worldPos: new THREE.Vector3(0.0, 1.05, 0.72),
                category: 'general',
                hasQuote: hasWipers,
                cameraPos: new THREE.Vector3(0.8, 1.5, 1.6),
                cameraLookAt: new THREE.Vector3(0.0, 0.92, 0.6)
            },
            {
                id: 'suspension',
                number: '3',
                label: 'Amortiguadores',
                subtitle: hasSuspension ? 'Fuga detectada / Alineación' : 'Suspensión calibrada',
                worldPos: new THREE.Vector3(0.94, 0.66, -0.65),
                category: 'safety',
                hasQuote: hasSuspension,
                cameraPos: new THREE.Vector3(1.9, 0.8, -0.4),
                cameraLookAt: new THREE.Vector3(0.75, 0.4, -0.65)
            },
            {
                id: 'engine',
                number: '4',
                label: 'Inspección Motor',
                subtitle: 'Niveles de fluidos y escaneo computarizado',
                worldPos: new THREE.Vector3(0.0, 0.92, 1.35),
                category: 'routine',
                hasQuote: false,
                cameraPos: new THREE.Vector3(0.0, 1.9, 2.1),
                cameraLookAt: new THREE.Vector3(0.0, 0.6, 1.2)
            }
        ];

        this.createHotspotElements();
    }

    createHotspotElements() {
        this.hotspotsOverlay.innerHTML = '';
        this.hotspots = [];

        this.hotspotsConfig.forEach((cfg) => {
            const el = document.createElement('div');
            el.className = `vehicle-hotspot-pin ${cfg.hasQuote ? 'has-action pulse-alert' : ''} ${cfg.category}`;
            el.id = `hotspot-${cfg.id}`;
            el.setAttribute('role', 'button');
            el.setAttribute('tabindex', '0');
            el.setAttribute('aria-label', `${cfg.label}: ${cfg.subtitle}`);

            el.innerHTML = `
                <div class="pin-marker">
                    <span class="pin-icon fw-bold" style="font-size:0.82rem;">${cfg.number}</span>
                    <span class="pin-pulse"></span>
                </div>
                <div class="pin-card">
                    <strong class="pin-title">${cfg.label}</strong>
                    <span class="pin-desc">${cfg.subtitle}</span>
                    <span class="pin-cta">Toca para enfocar inspección</span>
                </div>
            `;

            el.addEventListener('click', (e) => {
                e.stopPropagation();
                this.setActivePin(cfg.id);
                this.focusPart(cfg.id);
            });

            this.hotspotsOverlay.appendChild(el);

            this.hotspots.push({
                config: cfg,
                element: el
            });
        });
    }

    setActivePin(partKey) {
        this.hotspots.forEach(h => {
            if (h.config.id === partKey) {
                h.element.classList.add('active');
            } else {
                h.element.classList.remove('active');
            }
        });
    }

    updateHotspotsPositions() {
        if (!this.camera || !this.container) return;

        const width = this.container.clientWidth;
        const height = this.container.clientHeight;
        const halfW = width / 2;
        const halfH = height / 2;

        const tempV = new THREE.Vector3();

        this.hotspots.forEach((h) => {
            tempV.copy(h.config.worldPos);
            tempV.y += this.currentLiftHeight;

            tempV.project(this.camera);

            if (tempV.z > 1) {
                h.element.style.display = 'none';
                return;
            }

            h.element.style.display = 'block';
            const x = (tempV.x * halfW) + halfW;
            const y = -(tempV.y * halfH) + halfH;

            h.element.style.transform = `translate(-50%, -50%) translate3d(${Math.round(x)}px, ${Math.round(y)}px, 0)`;
        });
    }

    focusPart(partKey) {
        const targetCfg = this.hotspotsConfig.find(h => h.id === partKey);
        if (!targetCfg) return;

        this.activePartKey = partKey;
        this.setActivePin(partKey);

        const targetCamPos = targetCfg.cameraPos.clone();
        targetCamPos.y += this.currentLiftHeight;

        const targetLookAt = targetCfg.cameraLookAt.clone();
        targetLookAt.y += this.currentLiftHeight;

        this.animateCameraTo(targetCamPos, targetLookAt);
        this.pulseMeshHighlight(partKey);
        this.highlightFormCardForPart(partKey);

        if (typeof this.options.onPartSelected === 'function') {
            this.options.onPartSelected(partKey);
        }
    }

    resetCameraView() {
        const defaultCamPos = new THREE.Vector3(3.5, 1.7 + this.currentLiftHeight, 3.7);
        const defaultLookAt = new THREE.Vector3(0, 0.65 + this.currentLiftHeight, 0);
        this.animateCameraTo(defaultCamPos, defaultLookAt);
        this.activePartKey = null;
        this.setActivePin(null);
    }

    animateCameraTo(camPos, lookAtPos, duration = 850) {
        this.cameraStartPos = this.camera.position.clone();
        this.cameraTargetPos = camPos;
        this.controlsStartTarget = this.controls.target.clone();
        this.controlsTargetPos = lookAtPos;
        this.cameraAnimProgress = 0;
        this.cameraAnimDuration = duration;
        this.animatingCamera = true;
        this.cameraAnimStartTime = performance.now();
    }

    pulseMeshHighlight(partKey) {
        const meshGroup = this.highlightMeshes[partKey];
        if (!meshGroup) return;

        meshGroup.traverse(child => {
            if (child.isMesh && child.material) {
                const originalEmissive = child.material.emissive ? child.material.emissive.getHex() : 0x000000;
                const originalIntensity = child.material.emissiveIntensity || 0;

                child.material.emissive = new THREE.Color(0x38bdf8);
                child.material.emissiveIntensity = 1.5;

                setTimeout(() => {
                    child.material.emissive = new THREE.Color(originalEmissive);
                    child.material.emissiveIntensity = originalIntensity;
                }, 1300);
            }
        });
    }

    highlightFormCardForPart(partKey) {
        let selector = '';
        if (partKey === 'brakes') {
            selector = '[data-item-id*="101-1"], [data-item-id*="101-2"], [data-item-id*="103"]';
        } else if (partKey === 'wipers') {
            selector = '[data-item-id*="101-3"]';
        } else if (partKey === 'suspension') {
            selector = '[data-item-id*="102"]';
        }

        if (selector) {
            const cards = document.querySelectorAll(selector);
            cards.forEach(card => {
                card.classList.add('highlight-from-3d');
                card.scrollIntoView({ behavior: 'smooth', block: 'center' });
                setTimeout(() => card.classList.remove('highlight-from-3d'), 2500);
            });
        }
    }

    setLiftElevated(elevate, animate = true) {
        this.isLiftElevated = elevate;
        this.targetLiftHeight = elevate ? 0.95 : 0.0;
        if (!animate) {
            this.currentLiftHeight = this.targetLiftHeight;
            this.applyLiftHeight();
        }
    }

    toggleLift() {
        this.setLiftElevated(!this.isLiftElevated, true);
    }

    applyLiftHeight() {
        if (this.carGroup) {
            this.carGroup.position.y = this.currentLiftHeight;
        }
        if (this.liftArmsGroup) {
            this.liftArmsGroup.position.y = this.currentLiftHeight;
        }
        if (this.contactShadow) {
            const factor = Math.max(0.15, 0.75 - (this.currentLiftHeight * 0.5));
            this.contactShadow.material.opacity = factor;
            const scale = 1 + (this.currentLiftHeight * 0.2);
            this.contactShadow.scale.set(scale, scale, 1);
        }
    }

    updateHotspotStatus(partKey, decision) {
        const pin = document.getElementById(`hotspot-${partKey}`);
        if (!pin) return;

        pin.classList.remove('approved', 'rejected', 'pending');
        const descEl = pin.querySelector('.pin-desc');

        if (decision === 'approve') {
            pin.classList.add('approved');
            if (descEl) descEl.textContent = '✔️ Aprobado para cambio';
        } else if (decision === 'reject') {
            pin.classList.add('rejected');
            if (descEl) descEl.textContent = '✖️ Rechazado';
        } else {
            pin.classList.add('pending');
        }
    }

    setupEvents() {
        window.addEventListener('resize', () => this.onResize());

        // Permitir clic fuera para deseleccionar pin
        this.renderer.domElement.addEventListener('click', () => {
            this.setActivePin(null);
        });
    }

    onResize() {
        if (!this.container || !this.renderer || !this.camera) return;
        const width = this.container.clientWidth;
        const height = this.container.clientHeight || 380;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    animate() {
        requestAnimationFrame(() => this.animate());

        // Movimiento suave del elevador hidráulico
        if (Math.abs(this.currentLiftHeight - this.targetLiftHeight) > 0.004) {
            this.currentLiftHeight += (this.targetLiftHeight - this.currentLiftHeight) * 0.07;
            this.applyLiftHeight();
        }

        // Animación suave de cámara
        if (this.animatingCamera) {
            const now = performance.now();
            const elapsed = now - this.cameraAnimStartTime;
            let t = Math.min(1.0, elapsed / this.cameraAnimDuration);
            const ease = t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;

            this.camera.position.lerpVectors(this.cameraStartPos, this.cameraTargetPos, ease);
            this.controls.target.lerpVectors(this.controlsStartTarget, this.controlsTargetPos, ease);

            if (t >= 1.0) {
                this.animatingCamera = false;
            }
        }

        // Rotación pasiva muy sutil
        if (!this.animatingCamera && !this.activePartKey && this.controls && !this.controls.state) {
            this.carGroup.rotation.y += 0.001;
            this.liftArmsGroup.rotation.y = this.carGroup.rotation.y;
        }

        if (this.controls) {
            this.controls.update();
        }

        this.updateHotspotsPositions();
        this.renderer.render(this.scene, this.camera);
    }
}

window.Vehicle3DViewer = Vehicle3DViewer;
