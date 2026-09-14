import * as QRCode from 'qrcode';

/**
 * Standard ISO/IEC 18004 compliant QR Code Generator.
 * Produces real, scannable QR Codes that can be read by any phone camera or barcode scanner.
 */
export class QrCodeGenerator {
  /**
   * Generates a real, standard ISO/IEC 18004 decodable SVG string using the official QRCode engine.
   */
  public static async generateSvg(text: string, size: number = 120): Promise<string> {
    try {
      return await QRCode.toString(text, {
        type: 'svg',
        margin: 1,
        width: size,
        errorCorrectionLevel: 'M',
        color: {
          dark: '#000000',
          light: '#ffffff'
        }
      });
    } catch (err) {
      console.error('Error generating QR code SVG:', err);
      return this.generateSvgSync(text, size);
    }
  }

  /**
   * Synchronously generates an SVG string using the official QR Code matrix with Reed-Solomon error correction.
   */
  public static generateSvgSync(text: string, size: number = 120): string {
    try {
      const qr = QRCode.create(text, { errorCorrectionLevel: 'M' });
      const moduleCount = qr.modules.size;
      const margin = 1;
      const totalModules = moduleCount + margin * 2;
      const cellSize = size / totalModules;

      let paths = '';
      for (let r = 0; r < moduleCount; r++) {
        for (let c = 0; c < moduleCount; c++) {
          if (qr.modules.get(r, c)) {
            const x = ((c + margin) * cellSize).toFixed(2);
            const y = ((r + margin) * cellSize).toFixed(2);
            const w = (cellSize + 0.05).toFixed(2);
            paths += `<rect x="${x}" y="${y}" width="${w}" height="${w}" fill="#000000"/>`;
          }
        }
      }

      return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}" width="${size}" height="${size}" shape-rendering="crispEdges">
  <rect width="100%" height="100%" fill="#ffffff"/>
  ${paths}
</svg>`;
    } catch (err) {
      console.error('Error generating sync QR code:', err);
      return '';
    }
  }

  /**
   * Generates a real PNG base64 Data URL.
   */
  public static async generateDataUrl(text: string, size: number = 200): Promise<string> {
    try {
      return await QRCode.toDataURL(text, {
        margin: 1,
        width: size,
        errorCorrectionLevel: 'M',
        color: {
          dark: '#000000',
          light: '#ffffff'
        }
      });
    } catch (err) {
      console.error('Error generating QR DataURL:', err);
      return '';
    }
  }
}
