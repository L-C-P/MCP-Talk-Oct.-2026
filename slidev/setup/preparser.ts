// Slidev applies the preparser from the project root on the initial load only,
// so the audience filter must be wired in here. The actual logic lives in the
// reusable slidev-addon-audience-filter package.
import {createAudienceFilterPreparser} from 'slidev-addon-audience-filter'

export default createAudienceFilterPreparser()
