import AppKit
import Foundation

@main
enum MdmlApp {
    static func main() {
        let app = NSApplication.shared
        app.delegate = AppDelegate.shared
        app.setActivationPolicy(.accessory)
        app.run()
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    static let shared = AppDelegate()

    private var receivedFiles = false
    private var didConvert = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.servicesProvider = self
        NSUpdateDynamicServices()

        DispatchQueue.main.asyncAfter(deadline: .now() + 0.35) { [weak self] in
            guard let self, !self.receivedFiles else { return }
            self.showUsage()
            NSApp.terminate(nil)
        }
    }

    func application(_ application: NSApplication, open urls: [URL]) {
        convert(paths: urls.map(\.path))
    }

    func application(_ sender: NSApplication, openFile filename: String) -> Bool {
        convert(paths: [filename])
        return true
    }

    func application(_ sender: NSApplication, openFiles filenames: [String]) {
        convert(paths: filenames)
        sender.reply(toOpenOrPrint: .success)
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        true
    }

    @objc
    func convertToHtml(_ pasteboard: NSPasteboard, userData: String?, error: AutoreleasingUnsafeMutablePointer<NSString?>?) {
        convert(paths: Self.paths(from: pasteboard))
    }

    @discardableResult
    private func convert(paths: [String]) -> Bool {
        receivedFiles = true
        guard !didConvert else { return true }
        didConvert = true

        let files = paths.filter { !$0.isEmpty }
        guard !files.isEmpty else {
            showUsage()
            NSApp.terminate(nil)
            return false
        }

        guard let cli = Bundle.main.executableURL?
            .deletingLastPathComponent()
            .appendingPathComponent("mdml-cli")
        else {
            alert(title: "mdml", text: "Could not find the mdml converter.")
            NSApp.terminate(nil)
            return false
        }

        let process = Process()
        process.executableURL = cli
        process.arguments = files
        let stdout = Pipe()
        let stderr = Pipe()
        process.standardOutput = stdout
        process.standardError = stderr

        do {
            try process.run()
            process.waitUntilExit()
        } catch {
            alert(title: "mdml", text: "Could not start the converter.")
            NSApp.terminate(nil)
            return false
        }

        if process.terminationStatus == 0 {
            let count = files.count
            notify("Converted \(count) Markdown file\(count == 1 ? "" : "s") to HTML")
        } else {
            let message = String(data: stderr.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8)?
                .trimmingCharacters(in: .whitespacesAndNewlines)
            alert(title: "mdml", text: message?.isEmpty == false ? message! : "Conversion failed.")
        }

        NSApp.terminate(nil)
        return process.terminationStatus == 0
    }

    private func showUsage() {
        alert(
            title: "mdml",
            text: """
            Drop a Markdown file onto this app, or run mdml from Terminal.

            Example:
              mdml README.md --theme github

            In Finder, right-click a .md file and choose Convert to HTML.
            """
        )
    }

    private func alert(title: String, text: String) {
        NSApp.activate(ignoringOtherApps: true)
        let alert = NSAlert()
        alert.messageText = title
        alert.informativeText = text
        alert.alertStyle = .informational
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }

    private func notify(_ text: String) {
        let escaped = text.replacingOccurrences(of: "\\", with: "\\\\")
            .replacingOccurrences(of: "\"", with: "\\\"")
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/osascript")
        process.arguments = ["-e", "display notification \"\(escaped)\" with title \"mdml\""]
        try? process.run()
        process.waitUntilExit()
    }

    private static func paths(from pasteboard: NSPasteboard) -> [String] {
        if let urls = pasteboard.readObjects(
            forClasses: [NSURL.self],
            options: [.urlReadingFileURLsOnly: true]
        ) as? [URL] {
            return urls.map(\.path)
        }

        if let filenames = pasteboard.propertyList(
            forType: NSPasteboard.PasteboardType("NSFilenamesPboardType")
        ) as? [String] {
            return filenames
        }

        return []
    }
}
