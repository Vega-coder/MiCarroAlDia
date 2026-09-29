/**
 * Mi Carro al Día - Visor 3D Interactivo del Vehículo y Diagnóstico
 * Renderizado con Three.js: Carrocería estilizada, partes mecánicas visibles
 * (frenos de disco, pinzas, limpiaparabrisas, suspensión, motor), elevador hidráulico
 * de taller, marcadores interactivos (hotspots) y sincronización con cotizaciones.
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
        this.targetLiftHeight = (this.options.currentState === 'Diagnostico' || this.options.currentState === 'Reparacion') ? 1.0 : 0;
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
        this.setupCamera();
        this.setupLights();
        this.buildWorkshopEnvironment();
        this.buildCarModel();
        this.setupHotspots();
        this.setupEvents();
        this.animate();

        // Si el estado inicial es Diagnóstico o Reparación, elevar carro suavemente
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

        // Contenedor HTML para superposiciones de pines 3D (Hotspots)
        this.hotspotsOverlay = document.createElement('div');
        this.hotspotsOverlay.className = 'vehicle-hotspots-overlay';
        this.hotspotsOverlay.style.position = 'absolute';
        this.hotspotsOverlay.style.top = '0';
        this.hotspotsOverlay.style.left = '0';
        this.hotspotsOverlay.style.width = '100%';
        this.hotspotsOverlay.style.height = '100%';
        this.hotspotsOverlay.style.pointerEvents = 'none';
        this.container.appendChild(this.hotspotsOverlay);
    }

    setupScene() {
        this.scene = new THREE.Scene();
        // Fondo degradado suave de taller tecnológico
        this.scene.fog = new THREE.FogExp2(0x0f172a, 0.04);
    }

    setupCamera() {
        const width = this.container.clientWidth || 600;
        const height = this.container.clientHeight || 380;
        this.camera = new THREE.PerspectiveCamera(40, width / height, 0.1, 100);
        
        // Posición inicial estilizada en 3/4 frontal
        this.camera.position.set(3.4, 1.8, 3.6);

        if (typeof THREE.OrbitControls !== 'undefined') {
            this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.dampingFactor = 0.06;
            this.controls.minDistance = 2.0;
            this.controls.maxDistance = 7.5;
            this.controls.maxPolarAngle = Math.PI / 2 - 0.02; // No bajar del piso
            this.controls.target.set(0, 0.7, 0);
            this.controls.update();
        }
    }

    setupLights() {
        // Luz ambiental suave
        const ambientLight = new THREE.AmbientLight(0xf8fafc, 0.9);
        this.scene.add(ambientLight);

        // Luz principal cenital / direccional con sombras
        const mainLight = new THREE.DirectionalLight(0xffffff, 1.2);
        mainLight.position.set(4, 6, 4);
        mainLight.castShadow = true;
        mainLight.shadow.mapSize.width = 1024;
        mainLight.shadow.mapSize.height = 1024;
        mainLight.shadow.camera.near = 0.5;
        mainLight.shadow.camera.far = 15;
        mainLight.shadow.camera.left = -3;
        mainLight.shadow.camera.right = 3;
        mainLight.shadow.camera.top = 3;
        mainLight.shadow.camera.bottom = -3;
        mainLight.shadow.bias = -0.001;
        this.scene.add(mainLight);

        // Luz de relleno fría (estilo taller automotriz moderno)
        const fillLight = new THREE.DirectionalLight(0x38bdf8, 0.75);
        fillLight.position.set(-4, 3, -3);
        this.scene.add(fillLight);

        // Luz frontal de contraste
        const frontLight = new THREE.DirectionalLight(0xfffbeb, 0.5);
        frontLight.position.set(0, 2, 5);
        this.scene.add(frontLight);
    }

    buildWorkshopEnvironment() {
        // Piso de serviteca automotriz con cuadrícula y demarcación
        const floorGeo = new THREE.PlaneGeometry(16, 16);
        const floorMat = new THREE.MeshStandardMaterial({
            color: 0x0f172a,
            roughness: 0.75,
            metalness: 0.1
        });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.receiveShadow = true;
        this.scene.add(floor);

        // Cuadrícula estética del piso
        const gridHelper = new THREE.GridHelper(10, 20, 0x0284c7, 0x1e293b);
        gridHelper.position.y = 0.002;
        this.scene.add(gridHelper);

        // Sombra de contacto suave bajo el vehículo
        const shadowGeo = new THREE.PlaneGeometry(4.2, 2.2);
        const shadowMat = new THREE.MeshBasicMaterial({
            color: 0x000000,
            transparent: true,
            opacity: 0.45
        });
        this.contactShadow = new THREE.Mesh(shadowGeo, shadowMat);
        this.contactShadow.rotation.x = -Math.PI / 2;
        this.contactShadow.position.y = 0.005;
        this.scene.add(this.contactShadow);

        // Construir Elevador Hidráulico de Taller (Dos columnas amarillas de servicio)
        this.buildHydraulicLift();
    }

    buildHydraulicLift() {
        this.liftGroup = new THREE.Group();
        this.liftArmsGroup = new THREE.Group();

        const yellowSteelMat = new THREE.MeshStandardMaterial({
            color: 0xf59e0b,
            metalness: 0.5,
            roughness: 0.35
        });
        const darkSteelMat = new THREE.MeshStandardMaterial({
            color: 0x334155,
            metalness: 0.8,
            roughness: 0.25
        });

        // 2 Columnas laterales del elevador
        const colGeo = new THREE.BoxGeometry(0.24, 2.8, 0.32);
        const baseGeo = new THREE.BoxGeometry(0.55, 0.06, 0.65);

        // Columna Derecha
        const colRight = new THREE.Mesh(colGeo, yellowSteelMat);
        colRight.position.set(1.45, 1.4, 0);
        colRight.castShadow = true;
        const baseRight = new THREE.Mesh(baseGeo, darkSteelMat);
        baseRight.position.set(1.45, 0.03, 0);
        this.liftGroup.add(colRight, baseRight);

        // Columna Izquierda
        const colLeft = new THREE.Mesh(colGeo, yellowSteelMat);
        colLeft.position.set(-1.45, 1.4, 0);
        colLeft.castShadow = true;
        const baseLeft = new THREE.Mesh(baseGeo, darkSteelMat);
        baseLeft.position.set(-1.45, 0.03, 0);
        this.liftGroup.add(colLeft, baseLeft);

        // Brazos de elevación telescópicos que suben con el carro
        const armCrossGeo = new THREE.BoxGeometry(0.18, 0.12, 0.4);
        const carriageR = new THREE.Mesh(armCrossGeo, darkSteelMat);
        carriageR.position.set(1.33, 0.1, 0);
        const carriageL = new THREE.Mesh(armCrossGeo, darkSteelMat);
        carriageL.position.set(-1.33, 0.1, 0);

        // 4 brazos de soporte que entran bajo los largueros del carro
        const armBarGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.9, 8);
        armBarGeo.rotateZ(Math.PI / 2);

        const padGeo = new THREE.CylinderGeometry(0.08, 0.08, 0.04, 12);
        const rubberMat = new THREE.MeshStandardMaterial({ color: 0x111827, roughness: 0.9 });

        // Brazo frontal derecho
        const armFR = new THREE.Mesh(armBarGeo, yellowSteelMat);
        armFR.position.set(0.9, 0.1, 0.55);
        armFR.rotation.y = 0.5;
        const padFR = new THREE.Mesh(padGeo, rubberMat);
        padFR.position.set(0.55, 0.14, 0.75);

        // Brazo trasero derecho
        const armRR = new THREE.Mesh(armBarGeo, yellowSteelMat);
        armRR.position.set(0.9, 0.1, -0.55);
        armRR.rotation.y = -0.5;
        const padRR = new THREE.Mesh(padGeo, rubberMat);
        padRR.position.set(0.55, 0.14, -0.75);

        // Brazo frontal izquierdo
        const armFL = new THREE.Mesh(armBarGeo, yellowSteelMat);
        armFL.position.set(-0.9, 0.1, 0.55);
        armFL.rotation.y = -0.5;
        const padFL = new THREE.Mesh(padGeo, rubberMat);
        padFL.position.set(-0.55, 0.14, 0.75);

        // Brazo trasero izquierdo
        const armRL = new THREE.Mesh(armBarGeo, yellowSteelMat);
        armRL.position.set(-0.9, 0.1, -0.55);
        armRL.rotation.y = 0.5;
        const padRL = new THREE.Mesh(padGeo, rubberMat);
        padRL.position.set(-0.55, 0.14, -0.75);

        this.liftArmsGroup.add(carriageR, carriageL, armFR, armRR, armFL, armRL, padFR, padRR, padFL, padRL);
        this.scene.add(this.liftGroup);
        this.scene.add(this.liftArmsGroup);
    }

    buildCarModel() {
        this.carGroup = new THREE.Group();

        // Materiales automotrices
        // Pintura carrocería: Azul metalizado moderno elegante
        const paintMat = new THREE.MeshStandardMaterial({
            color: 0x1d4ed8, // Azul royal Autofrenos
            metalness: 0.85,
            roughness: 0.22,
            envMapIntensity: 1.2
        });
        this.carPaintMaterial = paintMat;

        // Vidrio polarizado reflectivo
        const glassMat = new THREE.MeshStandardMaterial({
            color: 0x0f172a,
            metalness: 0.2,
            roughness: 0.05,
            transparent: true,
            opacity: 0.75
        });

        // Plástico negro y molduras
        const blackTrimMat = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            metalness: 0.1,
            roughness: 0.8
        });

        // Cromo brillante
        const chromeMat = new THREE.MeshStandardMaterial({
            color: 0xf1f5f9,
            metalness: 0.95,
            roughness: 0.1
        });

        // Luces LED encendidas
        const headlightMat = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            emissive: 0xbae6fd,
            emissiveIntensity: 1.2,
            roughness: 0.1
        });

        const taillightMat = new THREE.MeshStandardMaterial({
            color: 0xef4444,
            emissive: 0xdc2626,
            emissiveIntensity: 1.5,
            roughness: 0.1
        });

        // ==========================================
        // 1. CARROCERÍA PRINCIPAL (CHASIS Y CABINA)
        // ==========================================
        // Chasis inferior / base
        const lowerBodyGeo = new THREE.BoxGeometry(1.68, 0.42, 3.8);
        const lowerBody = new THREE.Mesh(lowerBodyGeo, paintMat);
        lowerBody.position.set(0, 0.48, 0);
        lowerBody.castShadow = true;
        this.carGroup.add(lowerBody);

        // Capó delantero curvado
        const hoodGeo = new THREE.BoxGeometry(1.64, 0.22, 1.2);
        const hood = new THREE.Mesh(hoodGeo, paintMat);
        hood.position.set(0, 0.62, 1.2);
        hood.rotation.x = -0.06;
        hood.castShadow = true;
        this.carGroup.add(hood);

        // Cabina / Techo
        const cabinGeo = new THREE.BoxGeometry(1.48, 0.58, 2.0);
        const cabin = new THREE.Mesh(cabinGeo, paintMat);
        cabin.position.set(0, 0.88, -0.2);
        cabin.castShadow = true;
        this.carGroup.add(cabin);

        // Parabrisas delantero (Inclinado)
        const windshieldGeo = new THREE.PlaneGeometry(1.36, 0.72);
        const windshield = new THREE.Mesh(windshieldGeo, glassMat);
        windshield.position.set(0, 0.94, 0.72);
        windshield.rotation.x = -Math.PI / 4;
        this.carGroup.add(windshield);

        // Ventana trasera
        const rearWindowGeo = new THREE.PlaneGeometry(1.34, 0.65);
        const rearWindow = new THREE.Mesh(rearWindowGeo, glassMat);
        rearWindow.position.set(0, 0.94, -1.14);
        rearWindow.rotation.x = Math.PI / 4;
        rearWindow.rotation.y = Math.PI;
        this.carGroup.add(rearWindow);

        // Ventanas laterales
        const sideWindowGeo = new THREE.PlaneGeometry(1.8, 0.42);
        const sideWindowR = new THREE.Mesh(sideWindowGeo, glassMat);
        sideWindowR.position.set(0.75, 0.94, -0.2);
        sideWindowR.rotation.y = Math.PI / 2;
        const sideWindowL = new THREE.Mesh(sideWindowGeo, glassMat);
        sideWindowL.position.set(-0.75, 0.94, -0.2);
        sideWindowL.rotation.y = -Math.PI / 2;
        this.carGroup.add(sideWindowR, sideWindowL);

        // Parachoques delantero con rejilla
        const frontBumperGeo = new THREE.BoxGeometry(1.68, 0.32, 0.35);
        const frontBumper = new THREE.Mesh(frontBumperGeo, blackTrimMat);
        frontBumper.position.set(0, 0.38, 1.95);
        this.carGroup.add(frontBumper);

        const grillGeo = new THREE.PlaneGeometry(1.1, 0.18);
        const grill = new THREE.Mesh(grillGeo, chromeMat);
        grill.position.set(0, 0.42, 2.13);
        this.carGroup.add(grill);

        // Faros delanteros
        const headLightGeo = new THREE.BoxGeometry(0.32, 0.12, 0.1);
        const headLightR = new THREE.Mesh(headLightGeo, headlightMat);
        headLightR.position.set(0.62, 0.58, 2.02);
        const headLightL = new THREE.Mesh(headLightGeo, headlightMat);
        headLightL.position.set(-0.62, 0.58, 2.02);
        this.carGroup.add(headLightR, headLightL);

        // Luces traseras
        const tailLightGeo = new THREE.BoxGeometry(0.38, 0.12, 0.08);
        const tailLightR = new THREE.Mesh(tailLightGeo, taillightMat);
        tailLightR.position.set(0.6, 0.62, -1.91);
        const tailLightL = new THREE.Mesh(tailLightGeo, taillightMat);
        tailLightL.position.set(-0.6, 0.62, -1.91);
        this.carGroup.add(tailLightR, tailLightL);

        // Espejos retrovisores
        const mirrorGeo = new THREE.BoxGeometry(0.18, 0.09, 0.12);
        const mirrorR = new THREE.Mesh(mirrorGeo, paintMat);
        mirrorR.position.set(0.85, 0.88, 0.55);
        const mirrorL = new THREE.Mesh(mirrorGeo, paintMat);
        mirrorL.position.set(-0.85, 0.88, 0.55);
        this.carGroup.add(mirrorR, mirrorL);

        // ==========================================
        // 2. PARTE DESTACADA: LIMPIAPARABRISAS
        // ==========================================
        const wiperGroup = new THREE.Group();
        const wiperArmGeo = new THREE.CylinderGeometry(0.012, 0.012, 0.45, 6);
        wiperArmGeo.rotateZ(Math.PI / 2);
        const wiperMat = new THREE.MeshStandardMaterial({
            color: 0x0284c7, // Destacado sutil
            metalness: 0.8,
            roughness: 0.3
        });

        const wiper1 = new THREE.Mesh(wiperArmGeo, wiperMat);
        wiper1.position.set(0.28, 0.76, 0.94);
        wiper1.rotation.y = 0.4;
        wiper1.rotation.x = -0.7;

        const wiper2 = new THREE.Mesh(wiperArmGeo, wiperMat);
        wiper2.position.set(-0.28, 0.76, 0.94);
        wiper2.rotation.y = 0.4;
        wiper2.rotation.x = -0.7;

        wiperGroup.add(wiper1, wiper2);
        this.carGroup.add(wiperGroup);
        this.highlightMeshes['wipers'] = wiperGroup;

        // ==========================================
        // 3. RUEDAS Y MECÁNICA DE FRENOS VISIBLE
        // ==========================================
        this.wheels = [];
        this.brakeDiscs = [];
        this.brakeCalipers = [];

        const wheelPositions = [
            { x: 0.84, y: 0.33, z: 1.18, isFront: true, side: 'R' },   // Delantera Derecha
            { x: -0.84, y: 0.33, z: 1.18, isFront: true, side: 'L' },  // Delantera Izquierda
            { x: 0.84, y: 0.33, z: -1.18, isFront: false, side: 'R' }, // Trasera Derecha
            { x: -0.84, y: 0.33, z: -1.18, isFront: false, side: 'L' } // Trasera Izquierda
        ];

        // Geometría neumático
        const tireGeo = new THREE.CylinderGeometry(0.33, 0.33, 0.22, 24);
        tireGeo.rotateZ(Math.PI / 2);
        const tireMat = new THREE.MeshStandardMaterial({
            color: 0x18181b,
            roughness: 0.9,
            metalness: 0.05
        });

        // Rin deportivo de aleación
        const rimGeo = new THREE.CylinderGeometry(0.22, 0.22, 0.225, 16);
        rimGeo.rotateZ(Math.PI / 2);
        const rimMat = new THREE.MeshStandardMaterial({
            color: 0xd1d5db,
            metalness: 0.9,
            roughness: 0.2
        });

        // Disco de freno de acero ventilado (VISIBLE)
        const discGeo = new THREE.CylinderGeometry(0.18, 0.18, 0.02, 16);
        discGeo.rotateZ(Math.PI / 2);
        const discMat = new THREE.MeshStandardMaterial({
            color: 0x94a3b8,
            metalness: 0.95,
            roughness: 0.25
        });

        // Pinza de freno deportiva (Caliper rojo Autofrenos)
        const caliperGeo = new THREE.BoxGeometry(0.06, 0.12, 0.08);
        const caliperMat = new THREE.MeshStandardMaterial({
            color: 0xdc2626, // Rojo calipers
            metalness: 0.6,
            roughness: 0.3,
            emissive: 0x7f1d1d,
            emissiveIntensity: 0.4
        });

        // Resorte de suspensión visible
        const springGeo = new THREE.CylinderGeometry(0.05, 0.05, 0.32, 12);
        const springMat = new THREE.MeshStandardMaterial({
            color: 0xf59e0b, // Amarillo amortiguador
            metalness: 0.8,
            roughness: 0.3
        });

        const brakesGroup = new THREE.Group();
        const suspensionGroup = new THREE.Group();

        wheelPositions.forEach((wp) => {
            const wheelUnit = new THREE.Group();
            wheelUnit.position.set(wp.x, wp.y, wp.z);

            const tire = new THREE.Mesh(tireGeo, tireMat);
            tire.castShadow = true;
            const rim = new THREE.Mesh(rimGeo, rimMat);

            // Disco y mordaza
            const innerX = wp.side === 'R' ? -0.06 : 0.06;
            const disc = new THREE.Mesh(discGeo, discMat.clone());
            disc.position.x = innerX;

            const caliper = new THREE.Mesh(caliperGeo, caliperMat.clone());
            caliper.position.set(innerX, 0.1, 0.04);

            wheelUnit.add(tire, rim, disc, caliper);
            this.carGroup.add(wheelUnit);

            this.wheels.push(wheelUnit);
            this.brakeDiscs.push(disc);
            this.brakeCalipers.push(caliper);

            if (wp.isFront) {
                brakesGroup.add(disc, caliper);
            }

            // Resorte / amortiguador de suspensión arriba de la rueda
            const strut = new THREE.Mesh(springGeo, springMat);
            strut.position.set(wp.x * 0.75, wp.y + 0.25, wp.z);
            suspensionGroup.add(strut);
        });

        this.carGroup.add(suspensionGroup);
        this.highlightMeshes['brakes'] = brakesGroup;
        this.highlightMeshes['suspension'] = suspensionGroup;

        // ==========================================
        // 4. MOTOR / COMPARTIMIENTO BAJO EL CAPÓ
        // ==========================================
        const engineGroup = new THREE.Group();
        const blockGeo = new THREE.BoxGeometry(0.7, 0.28, 0.6);
        const blockMat = new THREE.MeshStandardMaterial({ color: 0x334155, metalness: 0.8, roughness: 0.3 });
        const engineBlock = new THREE.Mesh(blockGeo, blockMat);
        engineBlock.position.set(0, 0.48, 1.25);

        // Tapas y depósitos de líquidos
        const capGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.03, 8);
        const capOil = new THREE.Mesh(capGeo, new THREE.MeshStandardMaterial({ color: 0xf59e0b }));
        capOil.position.set(0.18, 0.64, 1.25);
        const capWater = new THREE.Mesh(capGeo, new THREE.MeshStandardMaterial({ color: 0x0284c7 }));
        capWater.position.set(-0.25, 0.64, 1.4);

        engineGroup.add(engineBlock, capOil, capWater);
        this.carGroup.add(engineGroup);
        this.highlightMeshes['engine'] = engineGroup;

        // Agregar todo el carro a la escena
        this.scene.add(this.carGroup);
    }

    setupHotspots() {
        // Mapeo inteligente de ítems y partes mecánicas
        // Analiza las cotizaciones actuales o el estado de avance
        const hasBrakes = this.options.items.some(i => i.name.toLowerCase().includes('freno') || i.name.toLowerCase().includes('pastilla') || i.name.toLowerCase().includes('disco'));
        const hasWipers = this.options.items.some(i => i.name.toLowerCase().includes('plumilla') || i.name.toLowerCase().includes('parabrisa'));
        const hasSuspension = this.options.items.some(i => i.name.toLowerCase().includes('amortiguador') || i.name.toLowerCase().includes('suspensión') || i.name.toLowerCase().includes('alineación'));

        this.hotspotsConfig = [
            {
                id: 'brakes',
                label: '🛡️ Frenos Delanteros',
                subtitle: hasBrakes ? 'Pastillas y discos desgastados (Seguridad)' : 'Sistema de frenos verificado',
                worldPos: new THREE.Vector3(0.96, 0.42, 1.18),
                category: 'safety',
                hasQuote: hasBrakes,
                cameraPos: new THREE.Vector3(1.7, 0.75, 1.7),
                cameraLookAt: new THREE.Vector3(0.85, 0.35, 1.18)
            },
            {
                id: 'wipers',
                label: '🌧️ Limpiaparabrisas',
                subtitle: hasWipers ? 'Plumillas de goma cristalizada' : 'Visibilidad óptima',
                worldPos: new THREE.Vector3(0.0, 1.08, 0.7),
                category: 'general',
                hasQuote: hasWipers,
                cameraPos: new THREE.Vector3(0.8, 1.5, 1.6),
                cameraLookAt: new THREE.Vector3(0.0, 0.92, 0.6)
            },
            {
                id: 'suspension',
                label: '🔩 Amortiguadores',
                subtitle: hasSuspension ? 'Fuga detectada / Alineación' : 'Suspensión calibrada',
                worldPos: new THREE.Vector3(0.92, 0.65, -0.6),
                category: 'safety',
                hasQuote: hasSuspension,
                cameraPos: new THREE.Vector3(1.9, 0.8, -0.4),
                cameraLookAt: new THREE.Vector3(0.75, 0.4, -0.6)
            },
            {
                id: 'engine',
                label: '⚙️ Inspección Motor',
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
            el.style.pointerEvents = 'auto';
            el.setAttribute('role', 'button');
            el.setAttribute('tabindex', '0');
            el.setAttribute('aria-label', `${cfg.label}: ${cfg.subtitle}`);

            el.innerHTML = `
                <div class="pin-marker">
                    <span class="pin-icon">${cfg.label.split(' ')[0]}</span>
                    <span class="pin-pulse"></span>
                </div>
                <div class="pin-card">
                    <strong class="pin-title">${cfg.label}</strong>
                    <span class="pin-desc">${cfg.subtitle}</span>
                    <span class="pin-cta">Toca para enfocar 🔍</span>
                </div>
            `;

            el.addEventListener('click', (e) => {
                e.stopPropagation();
                this.focusPart(cfg.id);
            });

            this.hotspotsOverlay.appendChild(el);

            this.hotspots.push({
                config: cfg,
                element: el
            });
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
            // Aplicar altura del elevador a la posición del punto
            tempV.copy(h.config.worldPos);
            tempV.y += this.currentLiftHeight;

            tempV.project(this.camera);

            // Si está detrás de la cámara, ocultar
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

        // Animar cámara suavemente a la posición preestablecida
        const targetCamPos = targetCfg.cameraPos.clone();
        targetCamPos.y += this.currentLiftHeight;

        const targetLookAt = targetCfg.cameraLookAt.clone();
        targetLookAt.y += this.currentLiftHeight;

        this.animateCameraTo(targetCamPos, targetLookAt);

        // Resaltar mesh en 3D (Efecto brillo emisivo temporal)
        this.pulseMeshHighlight(partKey);

        // Resaltar tarjeta correspondiente en el formulario
        this.highlightFormCardForPart(partKey);

        // Notificar callback si existe
        if (typeof this.options.onPartSelected === 'function') {
            this.options.onPartSelected(partKey);
        }
    }

    resetCameraView() {
        const defaultCamPos = new THREE.Vector3(3.4, 1.8 + this.currentLiftHeight, 3.6);
        const defaultLookAt = new THREE.Vector3(0, 0.7 + this.currentLiftHeight, 0);
        this.animateCameraTo(defaultCamPos, defaultLookAt);
        this.activePartKey = null;
    }

    animateCameraTo(camPos, lookAtPos, duration = 800) {
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
                child.material.emissiveIntensity = 1.4;

                setTimeout(() => {
                    child.material.emissive = new THREE.Color(originalEmissive);
                    child.material.emissiveIntensity = originalIntensity;
                }, 1200);
            }
        });
    }

    highlightFormCardForPart(partKey) {
        // Enlaza el componente 3D con las tarjetas de cotización en el DOM
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
        this.targetLiftHeight = elevate ? 1.05 : 0.0;
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
        // La sombra se desvanece suavemente cuando el carro sube en el elevador
        if (this.contactShadow) {
            const factor = Math.max(0.12, 0.45 - (this.currentLiftHeight * 0.3));
            this.contactShadow.material.opacity = factor;
            const scale = 1 + (this.currentLiftHeight * 0.15);
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

        // Permitir rotación táctil fluida sin bloquear el scroll de la página en móvil
        let touchStartY = 0;
        this.container.addEventListener('touchstart', (e) => {
            touchStartY = e.touches[0].clientY;
        }, { passive: true });
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

        // Interpolación del elevador hidráulico
        if (Math.abs(this.currentLiftHeight - this.targetLiftHeight) > 0.005) {
            this.currentLiftHeight += (this.targetLiftHeight - this.currentLiftHeight) * 0.08;
            this.applyLiftHeight();
        }

        // Interpolación suave de cámara cuando se enfoca una parte
        if (this.animatingCamera) {
            const now = performance.now();
            const elapsed = now - this.cameraAnimStartTime;
            let t = Math.min(1.0, elapsed / this.cameraAnimDuration);
            // Función easeInOutCubic
            const ease = t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;

            this.camera.position.lerpVectors(this.cameraStartPos, this.cameraTargetPos, ease);
            this.controls.target.lerpVectors(this.controlsStartTarget, this.controlsTargetPos, ease);

            if (t >= 1.0) {
                this.animatingCamera = false;
            }
        }

        // Rotación pasiva muy sutil cuando no se interactúa y está en vista general
        if (!this.animatingCamera && !this.activePartKey && this.controls && !this.controls.state) {
            // rotación sutil
            this.carGroup.rotation.y += 0.0012;
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
